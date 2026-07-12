namespace ScreenCapture.NET.Pipewire.Connector;

public interface IPipeWireFrameCapture
{
    Task<CapturedFrame> CaptureSingleFrameAsync(PortalCaptureSession session, CaptureRequest request, CancellationToken cancellationToken);
    NativeStreamCapture StreamCapture { get; }
}

public class CaptureRequest
{
    public CaptureRequest()
    {
        Fullscreen = true;
        X = 0;
        Y = 0;
        Height = 0;
        Width = 0;
    }
    
    public CaptureRequest(int x, int y, int width, int height)
    {
        Fullscreen = false;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    
    public bool Fullscreen { get; }
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
}

public sealed class PipeWireFrameCapture : IPipeWireFrameCapture
{
    private uint _currentNodeId;
    private readonly object _captureLock = new();

    public NativeStreamCapture StreamCapture { get; }

    public PipeWireFrameCapture(NativeStreamCapture streamCapture)
    {
        this.StreamCapture = streamCapture;
    }

    public async Task<CapturedFrame> CaptureSingleFrameAsync(PortalCaptureSession session, CaptureRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        lock (_captureLock)
        {
            StreamCapture.SetRegion(request.X, request.Y, request.Width, request.Height);
        }

        var frame = await StreamCapture.WaitForFrameAsync(5000, cancellationToken);
        if (frame == null)
        {
            throw new TimeoutException($"No frame received from PipeWire stream {session.StreamNode} within timeout");
        }

        return frame;
    }
}