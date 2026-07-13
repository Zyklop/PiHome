using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace ScreenCapture.NET.Pipewire.Connector;

/// <summary>
/// Managed wrapper over the native libpwcap.so shim. The shim negotiates the
/// format, forces SHM buffers, optionally crops in-place, and pushes each frame
/// via a native callback. We copy the frame inside the callback (it runs on the
/// PipeWire loop thread and the pointer is only valid for the call) and hand it
/// to a bounded channel that WaitForFrameAsync drains.
/// </summary>
public sealed class NativeStreamCapture : IDisposable
{
    private const string Lib = "pwcap"; // resolves libpwcap.so on the library path
 
    [StructLayout(LayoutKind.Sequential)]
    private struct PwcapFrame
    {
        public IntPtr Data;
        public int Width;
        public int Height;
        public int Stride;
        public int Size;
        public uint Format;
    }
 
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FrameCallback(ref PwcapFrame frame, IntPtr user);
 
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr pwcap_start(int pipewireFd, uint nodeId);
 
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void pwcap_set_callback(IntPtr handle, FrameCallback cb, IntPtr user);
 
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void pwcap_set_region(IntPtr handle, int x, int y, int w, int h);
 
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void pwcap_set_scale(IntPtr handle, int level);
 
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void pwcap_stop(IntPtr handle);
 
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr pwcap_last_error(IntPtr handle);
 
    private IntPtr _handle;
    private FrameCallback? _callback; // kept alive for the stream's lifetime
    private readonly Channel<CapturedFrame> _frames =
        Channel.CreateBounded<CapturedFrame>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true,
        });
 
    public void ConnectToStream(PortalCaptureSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
 
        _handle = pwcap_start(session.PipeWireFd, session.StreamNode);
        if (_handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("pwcap_start returned null (allocation failed).");
        }
 
        var err = Marshal.PtrToStringAnsi(pwcap_last_error(_handle));
        if (!string.IsNullOrEmpty(err))
        {
            var handle = _handle;
            _handle = IntPtr.Zero;
            pwcap_stop(handle);
            throw new InvalidOperationException($"PipeWire capture failed to start: {err}");
        }
 
        _callback = OnNativeFrame;
        pwcap_set_callback(_handle, _callback, IntPtr.Zero);
 
        Console.WriteLine($"Native capture started: fd={session.PipeWireFd}, node={session.StreamNode}");
    }
 
    /// <summary>Set the device-pixel crop region, or clear it with w/h &lt;= 0.</summary>
    public void SetRegion(int x, int y, int width, int height)
    {
        if (_handle != IntPtr.Zero)
        {
            pwcap_set_region(_handle, x, y, width, height);
        }
    }
 
    /// <summary>
    /// Set the downscale level applied after cropping. Output is scaled down by
    /// 2^level: 0 = full size, 1 = half, 2 = quarter, and so on.
    /// </summary>
    public void SetScale(int level)
    {
        if (_handle != IntPtr.Zero)
        {
            pwcap_set_scale(_handle, level);
        }
    }
 
    // Runs on the PipeWire loop thread. Copy fast, enqueue, return.
    private void OnNativeFrame(ref PwcapFrame frame, IntPtr user)
    {
        if (frame.Data == IntPtr.Zero || frame.Size <= 0)
        {
            return;
        }
 
        var managed = new byte[frame.Size];
        Marshal.Copy(frame.Data, managed, 0, frame.Size);
        var captured = new CapturedFrame(frame.Width, frame.Height, frame.Stride, managed, (SpaVideoFormat)frame.Format);
 
        // Bounded(1)+DropOldest: newest frame wins, writer never blocks the loop.
        _frames.Writer.TryWrite(captured);
        
        FrameReceived?.Invoke(captured);
    }
 
    public async Task<CapturedFrame?> WaitForFrameAsync(int timeoutMs, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);
        try
        {
            return await _frames.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null; // timed out
        }
    }
 
    public void Dispose()
    {
        var handle = _handle;
        _handle = IntPtr.Zero;
        if (handle != IntPtr.Zero)
        {
            pwcap_stop(handle); // stops the loop; no further callbacks after this returns
        }
        _callback = null;
        _frames.Writer.TryComplete();
    }

    
    public event Action<CapturedFrame>? FrameReceived;
}
    
    public enum SpaVideoFormat : uint
    {
        Unknown = 0,
        Encoded = 1,
        I420 = 2,
        YV12 = 3,
        YUY2 = 4,
        UYVY = 5,
        AYUV = 6,
        RGBx = 7,
        BGRx = 8,
        xRGB = 9,
        xBGR = 10,
        RGBA = 11,
        BGRA = 12,
        ARGB = 13,
        ABGR = 14,
        RGB = 15,
        BGR = 16,
        Y41B = 17,
        Y42B = 18,
        YVYU = 19,
        Y444 = 20,
        v210 = 21,
        v216 = 22,
        NV12 = 23,
        NV21 = 24,
        GRAY8 = 25,
        GRAY16_BE = 26,
        GRAY16_LE = 27,
        v308 = 28,
        RGB16 = 29,
        BGR16 = 30,
        RGB15 = 31,
        BGR15 = 32
    }
