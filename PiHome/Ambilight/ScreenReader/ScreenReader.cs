using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HPPH;
using ScreenCapture.NET;

namespace Ambilight.ScreenReader;

public class ScreenReader
{
    private readonly IScreenCaptureService screenCaptureService;
    private GraphicsCard? graphicsCard;
    private Display? display;
    private IScreenCapture screenCapture;
    private ICaptureZone? zone;

    public IEnumerable<(string Name, int DeviceId)> GraphicCards => screenCaptureService.GetGraphicsCards().Select(x => (x.Name, x.DeviceId));
    public IEnumerable<(string Name, int Index)> Displays => graphicsCard is null ? Enumerable.Empty<(string Name, int Index)>() : screenCaptureService.GetDisplays(graphicsCard.Value).Select(x => (x.DeviceName, x.Index));

    public ScreenReader()
    {
        if (OperatingSystem.IsLinux())
        {
            screenCaptureService = new X11ScreenCaptureService();
        }
        else if (OperatingSystem.IsWindows())
        {
            screenCaptureService = new DX11ScreenCaptureService();
        }
        else
        {
            throw new InvalidOperationException("Operating system not supported");
        }
    }

    public void SetGraphicCard(int deviceId)
    {
        graphicsCard = screenCaptureService.GetGraphicsCards().Single(x => x.DeviceId == deviceId);
    }

    public void SetDisplay(int index)
    {
        display = screenCaptureService.GetDisplays(graphicsCard.Value).Single(x => x.Index == index);
        screenCapture = screenCaptureService.GetScreenCapture(display.Value);
    }

    public ReadOnlySpan<byte> GetFullscreenImage2(out int width, out int height)
    {
        ICaptureZone fullscreen = screenCapture.RegisterCaptureZone(0, 0, screenCapture.Display.Width, screenCapture.Display.Height, 0);
        width = screenCapture.Display.Width;
        height = screenCapture.Display.Height;
        screenCapture.CaptureScreen();
        ReadOnlySpan<byte> data;
        using (fullscreen.Lock())
        {
            data = fullscreen.RawBuffer;
        }

        screenCapture.UnregisterCaptureZone(fullscreen);
        return data;
    }

    public void SetArea(double x, double y, double width, double height)
    {
        if (zone is not null)
        {
            screenCapture.UnregisterCaptureZone(zone);
        }

        zone = screenCapture.RegisterCaptureZone((int)(display.Value.Width * x), (int)(display.Value.Height * y),
            (int)(display.Value.Width * width), (int)(display.Value.Height * height), 2);
    }
}