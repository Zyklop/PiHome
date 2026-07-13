using ScreenCapture.NET.Pipewire.Connector;

namespace ScreenCapture.NET.Pipewire;

public class PipewireCaptureService : IScreenCaptureService
{
    private PortalScreenCastClient portalClient = new PortalScreenCastClient();
    private IPipeWireFrameCapture frameCapture;
    private PortalCaptureSession session;
    private NativeStreamCapture streamCapture;

    public IEnumerable<GraphicsCard> GetGraphicsCards()
    {
        if (session == null)
        {
            session = Task.Run(() => portalClient.StartSessionAsync(CancellationToken.None))
                .GetAwaiter().GetResult();
        }
        yield return new GraphicsCard(1, "Dummy", 1, 1);
    }

    public IEnumerable<Display> GetDisplays(GraphicsCard graphicsCard)
    {
        if (streamCapture == null)
        {
            streamCapture = new NativeStreamCapture();
            streamCapture.ConnectToStream(session);
            frameCapture = new PipeWireFrameCapture(streamCapture);
        }
        
        using var frame = Task.Run(() =>
                frameCapture.CaptureSingleFrameAsync(session, new CaptureRequest(), CancellationToken.None))
            .GetAwaiter().GetResult();

        yield return new Display(1, "Dummy", frame.Width, frame.Height, Rotation.None, graphicsCard);
    }

    public IScreenCapture GetScreenCapture(Display display)
    {
        return new PipewireScreenCapture(display, session, frameCapture);
    }
    
    public void Dispose()
    {
        portalClient?.Dispose();
        streamCapture?.Dispose();
    }
}