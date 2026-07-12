using HPPH;
using ScreenCapture.NET.Pipewire.Connector;

namespace ScreenCapture.NET.Pipewire;

public class PipewireCaptureZone : ICaptureZone, IDisposable
{
    NativeStreamCapture streamCapture;
    SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);
    private bool autoUpdate;
    private byte[] rawBuffer = [];

    public PipewireCaptureZone(NativeStreamCapture captureStreamCapture, Display display, int x, int y, int width, int height, int downscaleLevel)
    {
        streamCapture = captureStreamCapture;
        Display = display;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        DownscaleLevel = downscaleLevel;
        streamCapture.SetRegion(x, y, width, height);
        streamCapture.SetScale(downscaleLevel);
        streamCapture.FrameReceived += FrameCaptured;
        AutoUpdate = true;
    }

    private void FrameCaptured(CapturedFrame obj)
    {
        if (semaphore.CurrentCount == 0)
        {
            return;
        }
        semaphore.Wait();
        ColorFormat = MapFormat(obj.PixelFormat);
        Height = obj.Height;
        Width = obj.Width;
        Stride = obj.Stride;
        rawBuffer = obj.Data.ToArray();
        Updated?.Invoke(this, EventArgs.Empty);
        semaphore.Release();
    }

    private IColorFormat MapFormat(SpaVideoFormat spaFormat)
    {
        switch(spaFormat)
        {
            case SpaVideoFormat.Unknown:
            case SpaVideoFormat.Encoded:
            case SpaVideoFormat.I420:
            case SpaVideoFormat.AYUV:
            case SpaVideoFormat.UYVY:
            case SpaVideoFormat.YUY2:
            case SpaVideoFormat.YV12:
            case SpaVideoFormat.Y41B:
            case SpaVideoFormat.Y42B:
            case SpaVideoFormat.YVYU:
            case SpaVideoFormat.Y444:
            case SpaVideoFormat.v210:
            case SpaVideoFormat.v216:
            case SpaVideoFormat.NV12:
            case SpaVideoFormat.NV21:
            case SpaVideoFormat.GRAY8:
            case SpaVideoFormat.GRAY16_BE:
            case SpaVideoFormat.GRAY16_LE:
            case SpaVideoFormat.v308:
            case SpaVideoFormat.RGB16:
            case SpaVideoFormat.BGR16:
            case SpaVideoFormat.RGB15:
            case SpaVideoFormat.BGR15:
                throw new InvalidDataException("Unsupported SpaVideoFormat");
            case SpaVideoFormat.RGB:
                return ColorFormatRGB.Instance;
            case SpaVideoFormat.BGR:
                return ColorFormatBGR.Instance;
            case SpaVideoFormat.RGBA:
            case SpaVideoFormat.RGBx:
                return ColorFormatRGBA.Instance;
            case SpaVideoFormat.BGRA:
            case SpaVideoFormat.BGRx:
                return ColorFormatBGRA.Instance;
            case SpaVideoFormat.ARGB:
            case SpaVideoFormat.xRGB:
                return ColorFormatARGB.Instance;
            case SpaVideoFormat.ABGR:
            case SpaVideoFormat.xBGR:
                return ColorFormatABGR.Instance;
            default:
                throw new ArgumentOutOfRangeException(nameof(spaFormat), spaFormat, null);
        }
    }

    public void SetRegionAndScale(int? x, int? y, int? width, int? height, int? downScaleLevel)
    {
        X = x ?? X;
        Y = y ?? Y;
        Width = width ?? Width;
        Height = height ?? Height;
        DownscaleLevel = downScaleLevel ?? DownscaleLevel;
        streamCapture.SetRegion(X, Y, Width, Height);
        streamCapture.SetScale(DownscaleLevel);
    }

    public IDisposable Lock()
    {
        semaphore.Wait();
        return new DisposeLock(semaphore);
    }

    public void RequestUpdate()
    {
        throw new NotImplementedException();
    }

    public RefImage<TColor> GetRefImage<TColor>() where TColor : struct, IColor
    {
        throw new NotImplementedException();
    }

    public Display Display { get; }
    public IColorFormat ColorFormat { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Stride { get; private set; }
    public int DownscaleLevel { get; private set; }
    public int UnscaledWidth { get; }
    public int UnscaledHeight { get; }

    public ReadOnlySpan<byte> RawBuffer
    {
        get => new ReadOnlySpan<byte>(rawBuffer);
    }

    public IImage Image { get; }

    public bool AutoUpdate
    {
        get => autoUpdate;
        set
        {
            if (value && !autoUpdate)
            {
                streamCapture.FrameReceived += FrameCaptured;
            }
            else if (!value && autoUpdate)
            {
                streamCapture.FrameReceived -= FrameCaptured;
            }
            autoUpdate = value;
        }
    }

    public bool IsUpdateRequested { get; }
    public event EventHandler? Updated;

    private class DisposeLock : IDisposable
    {
        private SemaphoreSlim semaphore;

        public DisposeLock(SemaphoreSlim semaphore)
        {
            this.semaphore = semaphore;
        }

        public void Dispose()
        {
            semaphore.Release();
        }
    }

    public void Dispose()
    {
        streamCapture.FrameReceived -= FrameCaptured;
        semaphore.Dispose();
    }
}