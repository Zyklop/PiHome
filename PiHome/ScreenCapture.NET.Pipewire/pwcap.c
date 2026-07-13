// pwcap.c — minimal PipeWire screencast capture shim for CPU frame access.
//
// Owns a pw_thread_loop + fd-based core + stream. Negotiates a raw video
// format using the real SPA pod builder (so no hand-built byte layouts), and
// restricts the buffer dataType to mappable memory so the compositor delivers
// SHM buffers we can memcpy — no DMA-BUF / EGL needed.
//
// Delivery: register a callback with pwcap_set_callback() and frames are
// pushed from the PipeWire loop thread as they arrive. The callback must copy
// the data out and return promptly — the pointer is only valid for the
// duration of the call, and the call blocks the capture loop while it runs.
// A blocking pwcap_get_frame() poll API is also retained as a fallback.
//
// Cropping: pwcap_set_region() sets a device-pixel crop rectangle applied
// inside the shim before delivery. Coordinates are device pixels of the
// captured frame; any logical->device scaling must be done by the caller.
//
// Build:
//   cc -shared -fPIC -O2 -I/usr/include/pipewire-0.3 -I/usr/include/spa-0.2 \
//      -D_REENTRANT -fno-strict-aliasing -fno-strict-overflow \
//      -o libpwcap.so pwcap.c -lpipewire-0.3 -lpthread

#include <pipewire/pipewire.h>
#include <spa/param/video/format-utils.h>
#include <spa/param/props.h>

#include <pthread.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <time.h>
#include <stdint.h>

// Frame delivered to the callback / poll API. `data` is tightly packed
// (stride == width * 4) and valid only until the callback returns (callback
// mode) or until pwcap_release_frame (poll mode).
typedef struct {
    const uint8_t *data;
    int32_t width, height, stride, size;
    uint32_t format;
} pwcap_frame;

// Callback signature. `user` is the opaque pointer passed to pwcap_set_callback.
typedef void (*pwcap_frame_cb)(const pwcap_frame *frame, void *user);

struct pwcap {
    struct pw_thread_loop *loop;
    struct pw_context     *context;
    struct pw_core        *core;
    struct pw_stream      *stream;
    struct spa_hook        stream_listener;

    struct spa_video_info_raw fmt;   // negotiated format
    int have_format;

    // Crop region in device pixels + downscale level. Guarded by region_lock.
    // scale_level L means a linear downscale factor of (1 << L): 0 = full,
    // 1 = half, 2 = quarter, ...
    pthread_mutex_t region_lock;
    int have_region;
    int rx, ry, rw, rh;
    int scale_level;

    // Callback delivery.
    pwcap_frame_cb cb;
    void *cb_user;

    // Holding buffer (tightly packed BGRA/etc). Reused across frames; also
    // backs the poll API. Sized to the delivered (possibly cropped) frame.
    pthread_mutex_t lock;
    pthread_cond_t  cond;
    uint8_t *frame;
    size_t   frame_cap;
    int      frame_w, frame_h, frame_stride, frame_size;
    uint32_t frame_format;
    int      have_frame;   // 1 = frame ready and owned by caller until release
    int      errored;
    char     err[256];
};

static void set_err(struct pwcap *c, const char *msg) {
    pthread_mutex_lock(&c->lock);
    snprintf(c->err, sizeof(c->err), "%s", msg ? msg : "unknown error");
    c->errored = 1;
    pthread_cond_signal(&c->cond);
    pthread_mutex_unlock(&c->lock);
}

static void on_state_changed(void *data, enum pw_stream_state old,
                             enum pw_stream_state state, const char *error) {
    struct pwcap *c = data;
    (void)old;
    if (state == PW_STREAM_STATE_ERROR)
        set_err(c, error ? error : "stream error");
}

static void on_param_changed(void *data, uint32_t id, const struct spa_pod *param) {
    struct pwcap *c = data;
    if (param == NULL || id != SPA_PARAM_Format)
        return;

    uint32_t mtype, msubtype;
    if (spa_format_parse(param, &mtype, &msubtype) < 0)
        return;
    if (mtype != SPA_MEDIA_TYPE_video || msubtype != SPA_MEDIA_SUBTYPE_raw)
        return;

    spa_zero(c->fmt);
    if (spa_format_video_raw_parse(param, &c->fmt) < 0)
        return;
    c->have_format = 1;

    int32_t w = c->fmt.size.width;
    int32_t h = c->fmt.size.height;
    int32_t stride = w * 4;
    int32_t size = stride * h;

    uint8_t buf[1024];
    struct spa_pod_builder b = SPA_POD_BUILDER_INIT(buf, sizeof(buf));
    const struct spa_pod *params[1];

    // Request several buffers, 1 block, and — crucially — only mappable
    // memory types. This forces the SHM path even though kwin can do DMA-BUF.
    params[0] = spa_pod_builder_add_object(&b,
        SPA_TYPE_OBJECT_ParamBuffers, SPA_PARAM_Buffers,
        SPA_PARAM_BUFFERS_buffers,  SPA_POD_CHOICE_RANGE_Int(4, 2, 8),
        SPA_PARAM_BUFFERS_blocks,   SPA_POD_Int(1),
        SPA_PARAM_BUFFERS_size,     SPA_POD_Int(size),
        SPA_PARAM_BUFFERS_stride,   SPA_POD_Int(stride),
        SPA_PARAM_BUFFERS_dataType, SPA_POD_CHOICE_FLAGS_Int(
            (1 << SPA_DATA_MemFd) | (1 << SPA_DATA_MemPtr)));

    pw_stream_update_params(c->stream, params, 1);
}

// Copies the source frame into c->frame, applying the crop region and then a
// box-average downscale by 2^scale_level. Sets the frame_* fields and returns 1
// on success. Caller must hold c->lock.
static int stage_frame(struct pwcap *c, const uint8_t *src, int src_stride,
                       int full_w, int full_h) {
    const int bpp = 4;

    // Snapshot region + scale under their own lock so a concurrent setter
    // can't change dimensions mid-copy.
    int use_region, rx, ry, rw, rh, level;
    pthread_mutex_lock(&c->region_lock);
    use_region = c->have_region;
    rx = c->rx; ry = c->ry; rw = c->rw; rh = c->rh;
    level = c->scale_level;
    pthread_mutex_unlock(&c->region_lock);

    // Crop rectangle in source pixels.
    int out_x = 0, out_y = 0, out_w = full_w, out_h = full_h;
    if (use_region) {
        if (rx < 0) rx = 0;
        if (ry < 0) ry = 0;
        if (rw <= 0 || rh <= 0) return 0;
        if (rx >= full_w || ry >= full_h) return 0;
        if (rx + rw > full_w) rw = full_w - rx;
        if (ry + rh > full_h) rh = full_h - ry;
        out_x = rx; out_y = ry; out_w = rw; out_h = rh;
    }

    // Downscale factor = 2^level. For a plain integer divisor instead, replace
    // the next two lines with: int factor = level > 1 ? level : 1;
    int factor = 1;
    if (level > 0) factor = 1 << level;
    if (factor < 1) factor = 1;

    // Output dimensions. Remainder columns/rows that don't fill a full block
    // are truncated.
    int dst_w = out_w / factor;
    int dst_h = out_h / factor;
    if (dst_w <= 0 || dst_h <= 0) return 0;

    int dst_stride = dst_w * bpp;
    size_t need = (size_t)dst_stride * (size_t)dst_h;
    if (need == 0) return 0;

    if (c->frame_cap < need) {
        free(c->frame);
        c->frame = malloc(need);
        c->frame_cap = c->frame ? need : 0;
    }
    if (!c->frame) return 0;

    if (factor == 1) {
        // Crop only: copy rows straight through.
        for (int y = 0; y < dst_h; y++) {
            const uint8_t *srow = src + (size_t)(out_y + y) * src_stride + (size_t)out_x * bpp;
            memcpy(c->frame + (size_t)y * dst_stride, srow, dst_stride);
        }
    } else {
        // Crop + box-average each factor x factor block into one output pixel.
        int area = factor * factor;
        for (int oy = 0; oy < dst_h; oy++) {
            uint8_t *drow = c->frame + (size_t)oy * dst_stride;
            int sy0 = out_y + oy * factor;
            for (int ox = 0; ox < dst_w; ox++) {
                int sx0 = out_x + ox * factor;
                unsigned s0 = 0, s1 = 0, s2 = 0, s3 = 0;
                for (int yy = 0; yy < factor; yy++) {
                    const uint8_t *p = src + (size_t)(sy0 + yy) * src_stride + (size_t)sx0 * bpp;
                    for (int xx = 0; xx < factor; xx++) {
                        s0 += p[0]; s1 += p[1]; s2 += p[2]; s3 += p[3];
                        p += bpp;
                    }
                }
                uint8_t *dp = drow + (size_t)ox * bpp;
                dp[0] = (uint8_t)(s0 / area);
                dp[1] = (uint8_t)(s1 / area);
                dp[2] = (uint8_t)(s2 / area);
                dp[3] = (uint8_t)(s3 / area);
            }
        }
    }

    c->frame_w = dst_w;
    c->frame_h = dst_h;
    c->frame_stride = dst_stride;
    c->frame_size = (int)need;
    c->frame_format = c->fmt.format;
    return 1;
}

static void on_process(void *data) {
    struct pwcap *c = data;
    struct pw_buffer *b = pw_stream_dequeue_buffer(c->stream);
    if (b == NULL)
        return;

    struct spa_buffer *sb = b->buffer;
    if (sb->n_datas < 1) { pw_stream_queue_buffer(c->stream, b); return; }

    struct spa_data *d = &sb->datas[0];
    struct spa_chunk *chunk = d->chunk;
    if (d->data == NULL || chunk == NULL || chunk->size == 0) {
        pw_stream_queue_buffer(c->stream, b);
        return;
    }

    int full_w = c->fmt.size.width;
    int full_h = c->fmt.size.height;
    int src_stride = chunk->stride > 0 ? chunk->stride : full_w * 4;
    const uint8_t *src = (const uint8_t *)d->data + chunk->offset;

    if (c->have_format && full_w > 0 && full_h > 0) {
        pthread_mutex_lock(&c->lock);

        pwcap_frame_cb cb = c->cb;
        void *cb_user = c->cb_user;

        if (cb != NULL) {
            // Callback mode: always stage the newest frame and deliver it.
            if (stage_frame(c, src, src_stride, full_w, full_h)) {
                pwcap_frame f = {
                    .data = c->frame,
                    .width = c->frame_w,
                    .height = c->frame_h,
                    .stride = c->frame_stride,
                    .size = c->frame_size,
                    .format = c->frame_format,
                };
                // Invoke while holding the lock so the staging buffer can't be
                // reused underneath the callback. The callback must copy and
                // return promptly.
                cb(&f, cb_user);
            }
        } else if (!c->have_frame) {
            // Poll mode: stage only when the previous frame was released.
            if (stage_frame(c, src, src_stride, full_w, full_h)) {
                c->have_frame = 1;
                pthread_cond_signal(&c->cond);
            }
        }

        pthread_mutex_unlock(&c->lock);
    }

    pw_stream_queue_buffer(c->stream, b);
}

static const struct pw_stream_events stream_events = {
    PW_VERSION_STREAM_EVENTS,
    .state_changed = on_state_changed,
    .param_changed = on_param_changed,
    .process = on_process,
};

struct pwcap *pwcap_start(int pipewire_fd, uint32_t node_id) {
    pw_init(NULL, NULL);

    struct pwcap *c = calloc(1, sizeof(*c));
    if (!c) return NULL;
    pthread_mutex_init(&c->lock, NULL);
    pthread_mutex_init(&c->region_lock, NULL);
    pthread_cond_init(&c->cond, NULL);

    c->loop = pw_thread_loop_new("pwcap", NULL);
    if (!c->loop) { set_err(c, "thread_loop_new failed"); return c; }
    if (pw_thread_loop_start(c->loop) < 0) { set_err(c, "thread_loop_start failed"); return c; }

    pw_thread_loop_lock(c->loop);

    c->context = pw_context_new(pw_thread_loop_get_loop(c->loop), NULL, 0);
    if (!c->context) { pw_thread_loop_unlock(c->loop); set_err(c, "context_new failed"); return c; }

    c->core = pw_context_connect_fd(c->context, pipewire_fd, NULL, 0);
    if (!c->core) { pw_thread_loop_unlock(c->loop); set_err(c, "connect_fd failed"); return c; }

    struct pw_properties *props = pw_properties_new(
        PW_KEY_MEDIA_TYPE, "Video",
        PW_KEY_MEDIA_CATEGORY, "Capture",
        PW_KEY_MEDIA_ROLE, "Screen",
        NULL);

    c->stream = pw_stream_new(c->core, "pwcap-capture", props);
    if (!c->stream) { pw_thread_loop_unlock(c->loop); set_err(c, "stream_new failed"); return c; }

    pw_stream_add_listener(c->stream, &c->stream_listener, &stream_events, c);

    // Offer several 4-byte RGB formats with no modifier. Combined with the
    // MemFd/MemPtr dataType restriction above, this lands on the SHM path.
    uint8_t buf[1024];
    struct spa_pod_builder b = SPA_POD_BUILDER_INIT(buf, sizeof(buf));
    const struct spa_pod *params[1];
    params[0] = spa_pod_builder_add_object(&b,
        SPA_TYPE_OBJECT_Format, SPA_PARAM_EnumFormat,
        SPA_FORMAT_mediaType,    SPA_POD_Id(SPA_MEDIA_TYPE_video),
        SPA_FORMAT_mediaSubtype, SPA_POD_Id(SPA_MEDIA_SUBTYPE_raw),
        SPA_FORMAT_VIDEO_format, SPA_POD_CHOICE_ENUM_Id(5,
            SPA_VIDEO_FORMAT_BGRA,   // default
            SPA_VIDEO_FORMAT_BGRA, SPA_VIDEO_FORMAT_BGRx,
            SPA_VIDEO_FORMAT_RGBA, SPA_VIDEO_FORMAT_RGBx),
        SPA_FORMAT_VIDEO_size, SPA_POD_CHOICE_RANGE_Rectangle(
            &SPA_RECTANGLE(1920, 1080),
            &SPA_RECTANGLE(1, 1),
            &SPA_RECTANGLE(8192, 8192)),
        SPA_FORMAT_VIDEO_framerate, SPA_POD_CHOICE_RANGE_Fraction(
            &SPA_FRACTION(0, 1),
            &SPA_FRACTION(0, 1),
            &SPA_FRACTION(1000, 1)));

    int res = pw_stream_connect(c->stream, PW_DIRECTION_INPUT, node_id,
        PW_STREAM_FLAG_AUTOCONNECT | PW_STREAM_FLAG_MAP_BUFFERS,
        params, 1);

    pw_thread_loop_unlock(c->loop);

    if (res < 0) { set_err(c, "stream_connect failed"); return c; }
    return c;
}

// Register (or clear, with cb == NULL) the frame callback. Must be called
// before frames start flowing for deterministic delivery; safe to call after.
void pwcap_set_callback(struct pwcap *c, pwcap_frame_cb cb, void *user) {
    if (!c) return;
    pthread_mutex_lock(&c->lock);
    c->cb = cb;
    c->cb_user = user;
    pthread_mutex_unlock(&c->lock);
}

// Set the device-pixel crop region. Pass w <= 0 or h <= 0 to clear (full frame).
void pwcap_set_region(struct pwcap *c, int x, int y, int w, int h) {
    if (!c) return;
    pthread_mutex_lock(&c->region_lock);
    if (w > 0 && h > 0) {
        c->have_region = 1;
        c->rx = x; c->ry = y; c->rw = w; c->rh = h;
    } else {
        c->have_region = 0;
    }
    pthread_mutex_unlock(&c->region_lock);
}

// Set the downscale level. Output is downscaled by 2^level after cropping:
// 0 = full size, 1 = half, 2 = quarter, and so on. Negative values clamp to 0.
void pwcap_set_scale(struct pwcap *c, int level) {
    if (!c) return;
    if (level < 0) level = 0;
    pthread_mutex_lock(&c->region_lock);
    c->scale_level = level;
    pthread_mutex_unlock(&c->region_lock);
}

// Blocking poll fallback. Returns 0 and fills *out on success (out->data valid
// until pwcap_release_frame), -1 on timeout/error. Do not mix with callback mode.
int pwcap_get_frame(struct pwcap *c, pwcap_frame *out, int timeout_ms) {
    if (!c || !out) return -1;

    struct timespec ts;
    clock_gettime(CLOCK_REALTIME, &ts);
    ts.tv_sec  += timeout_ms / 1000;
    ts.tv_nsec += (long)(timeout_ms % 1000) * 1000000L;
    if (ts.tv_nsec >= 1000000000L) { ts.tv_sec++; ts.tv_nsec -= 1000000000L; }

    pthread_mutex_lock(&c->lock);
    int rc = 0;
    while (!c->have_frame && !c->errored && rc == 0)
        rc = pthread_cond_timedwait(&c->cond, &c->lock, &ts);

    int ret = -1;
    if (c->have_frame) {
        out->data   = c->frame;
        out->width  = c->frame_w;
        out->height = c->frame_h;
        out->stride = c->frame_stride;
        out->size   = c->frame_size;
        out->format = c->frame_format;
        ret = 0;   // leave have_frame set until release
    }
    pthread_mutex_unlock(&c->lock);
    return ret;
}

void pwcap_release_frame(struct pwcap *c) {
    if (!c) return;
    pthread_mutex_lock(&c->lock);
    c->have_frame = 0;
    pthread_mutex_unlock(&c->lock);
}

const char *pwcap_last_error(struct pwcap *c) {
    return c ? c->err : "null handle";
}

void pwcap_stop(struct pwcap *c) {
    if (!c) return;
    if (c->loop) pw_thread_loop_lock(c->loop);
    if (c->stream) { pw_stream_destroy(c->stream); c->stream = NULL; }
    if (c->core)   { pw_core_disconnect(c->core);  c->core = NULL; }
    if (c->context){ pw_context_destroy(c->context); c->context = NULL; }
    if (c->loop) {
        pw_thread_loop_unlock(c->loop);
        pw_thread_loop_stop(c->loop);
        pw_thread_loop_destroy(c->loop);
        c->loop = NULL;
    }
    free(c->frame);
    pthread_mutex_destroy(&c->lock);
    pthread_mutex_destroy(&c->region_lock);
    pthread_cond_destroy(&c->cond);
    free(c);
    pw_deinit();
}