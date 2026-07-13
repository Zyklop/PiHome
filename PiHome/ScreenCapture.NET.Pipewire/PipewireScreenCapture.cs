namespace ScreenCapture.NET.Pipewire.Connector;

public class PipewireScreenCapture : IScreenCapture
{
    private PortalCaptureSession Session;
    private readonly IPipeWireFrameCapture Capture;
    private PipewireCaptureZone Zone;

    public PipewireScreenCapture(Display display, PortalCaptureSession session, IPipeWireFrameCapture frameCapture)
    {
        Display = display;
        Session = session;
        Capture = frameCapture;
    }

    public bool CaptureScreen()
    {
        var frame = Capture.CaptureSingleFrameAsync(Session, new CaptureRequest(), CancellationToken.None).Result;
        if (frame.ByteLength > 1000)
        {
            return true;
        }

        return false;
    }

    public ICaptureZone RegisterCaptureZone(int x, int y, int width, int height, int downscaleLevel = 0)
    {
        if (Zone != null)
        {
            throw new Exception("Zone already registered, only one zone is supported");
        }

        Zone = new PipewireCaptureZone(Capture.StreamCapture, Display, x,  y, width, height, downscaleLevel);
        Capture.StreamCapture.SetRegion(x, y, width, height);
        Capture.StreamCapture.SetScale(downscaleLevel);
        return Zone;
    }

    public bool UnregisterCaptureZone(ICaptureZone captureZone)
    {
        Zone?.Dispose();
        Zone = null;
        return true;
    }

    public void UpdateCaptureZone(ICaptureZone captureZone, int? x = null, int? y = null, int? width = null, int? height = null,
        int? downscaleLevel = null)
    {
        Zone.SetRegionAndScale(x, y, width, height, downscaleLevel);
    }

    public void Restart()
    {
        throw new NotImplementedException();
    }

    public Display Display { get; }
    public event EventHandler<ScreenCaptureUpdatedEventArgs>? Updated;
    
    public void Dispose()
    {
    }

}