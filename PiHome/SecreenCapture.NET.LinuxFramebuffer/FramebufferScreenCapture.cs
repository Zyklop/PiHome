using System.Diagnostics;
using System.Globalization;
using HPPH;
using ScreenCapture.NET;

namespace SecreenCapture.NET.LinuxFramebuffer;

public class FramebufferScreenCapture : IScreenCapture
{
    private MemoryStream buffer;
    private int stride;
    private int bitsPerPixel;
    private List<FrameBufferCaptureZone> zones = new();
    private byte[] pixelBuffer;

    public FramebufferScreenCapture(Display display)
    {
        Display = display;
        buffer = new MemoryStream();
        
        using var fs = File.OpenText($"/sys/class/graphics/{Display.GraphicsCard.Name}/stride");
        if (!int.TryParse(fs.ReadLine(), out stride))
        {
            throw new Exception("Failed to read stride");
        }
        using var fs2 = File.OpenText($"/sys/class/graphics/{Display.GraphicsCard.Name}/bits_per_pixel");
        if (!int.TryParse(fs2.ReadLine(), out bitsPerPixel))
        {
            throw new Exception("Failed to read bitsperpixel");
        }

        pixelBuffer = new byte[bitsPerPixel / 8];
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public bool CaptureScreen()
    {
        var fs = new FileStream("/dev/fb0", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var done = new bool[zones.Count];
        for (int y = 0; y < Display.Height; y++)
        {
            for (int x = 0; x < stride/(pixelBuffer.Length); x++)
            {
                fs.Read(pixelBuffer);
                if (pixelBuffer[1] > 0)
                {
                    int d = 1;
                }
                for (var i = 0; i < zones.Count; i++)
                {
                    var captureZone = zones[i];
                    if (x >= captureZone.X && y >= captureZone.Y && x < captureZone.Width + captureZone.X && y < captureZone.Height + captureZone.Y)
                    {
                        var yRel = y - captureZone.Y;
                        var xRel = x - captureZone.X;
                        var startPos = (yRel * captureZone.Width + xRel) * 4;
                        captureZone.buffer[startPos] = pixelBuffer[0];
                        captureZone.buffer[startPos + 1] = pixelBuffer[1];
                        captureZone.buffer[startPos + 2] = pixelBuffer[2];
                        captureZone.buffer[startPos + 3] = 255;
                    }
                    else if (x >= captureZone.Width + captureZone.X && y >= captureZone.Height + captureZone.Y)
                    {
                        done[i] = true;
                    }
                }
            }

            if (done.All(b => b))
            {
                break;
            }
        }
        return true;
    }

    public ICaptureZone RegisterCaptureZone(int x, int y, int width, int height, int downscaleLevel = 0)
    {
        if (downscaleLevel != 0)
        {
            throw new ArgumentException("Downscaling not possible");
        }

        var zone = new FrameBufferCaptureZone(Display, IColorFormat.RGBA, x, y, width, height, width * (bitsPerPixel / 8));
        zones.Add(zone);
        return zone;
    }

    public bool UnregisterCaptureZone(ICaptureZone captureZone)
    {
        return zones.Remove((FrameBufferCaptureZone)captureZone);
    }

    public void UpdateCaptureZone(ICaptureZone captureZone, int? x = null, int? y = null, int? width = null, int? height = null,
        int? downscaleLevel = null)
    {
        throw new NotImplementedException();
    }

    public void Restart()
    {
        throw new NotImplementedException();
    }

    public Display Display { get; }
    public event EventHandler<ScreenCaptureUpdatedEventArgs>? Updated;
}