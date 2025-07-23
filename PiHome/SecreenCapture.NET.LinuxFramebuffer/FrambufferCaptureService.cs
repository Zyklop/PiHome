using ScreenCapture.NET;

namespace SecreenCapture.NET.LinuxFramebuffer;

public class FrambufferCaptureService: IScreenCaptureService
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<GraphicsCard> GetGraphicsCards()
    {
        var di = new DirectoryInfo("/dev");
        var index = 0;
        foreach (var fb in di.GetFiles("fb*"))
        {
            yield return new GraphicsCard(index, fb.Name, 42, index++);
        }
    }

    public IEnumerable<Display> GetDisplays(GraphicsCard graphicsCard)
    {
        using var fs = File.OpenText($"/sys/class/graphics/{graphicsCard.Name}/virtual_size");
        var res = fs.ReadToEnd().Split(',');
        if (int.TryParse(res[0].Trim(), out var width) && int.TryParse(res[1].Trim(), out var height))
        {
            yield return new Display(0, "Global", width, height, Rotation.None, graphicsCard);
        }
    }

    public IScreenCapture GetScreenCapture(Display display)
    {
        return new FramebufferScreenCapture(display);
    }
}