using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Communication.ApiCommunication;
using HPPH;
using ScreenCapture.NET;

namespace Ambilight.ScreenReader;

public class ScreenReader
{
    private readonly IScreenCaptureService screenCaptureService;
    private readonly CommunicatorFactory commFac = new();
    private GraphicsCard? graphicsCard;
    private Display? display;
    private IScreenCapture screenCapture;
    private ICaptureZone? zone;
    private bool isRunning;
    private double xZone;
    private double yZone;
    private double zoneWidth;
    private double zoneHeight;
    private CancellationTokenSource? tokenSource;

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
        if (isRunning)
        {
            throw new InvalidOperationException("Running please stop first");
        }

        xZone = (display.Value.Width * x);
        yZone = (display.Value.Height * y);
        zoneWidth = (display.Value.Width * width);
        zoneHeight = (display.Value.Height * height);
    }

    public void Start(IPAddress ip, int start, int end, byte brightness = 13, bool flip = false)
    {
        if (isRunning)
        {
            return;
        }
        Task.Run(() =>
        {
            isRunning = true;
            tokenSource = new CancellationTokenSource();
            var comm = commFac.GetLedCommunicator(ip);
            var sendBuffer = new byte[end * 4];
            var numLeds = end - start;
            var sumBuffer = new ulong[numLeds * 3];
            var rowPerLed = new int[numLeds];
            for (int i = start; i < end; i++)
            {
                sendBuffer[i * 4 + 3] = brightness;
            }
            var scale = Math.Log2(zoneWidth / numLeds);
            zone = screenCapture.RegisterCaptureZone((int)xZone, (int)yZone, (int)zoneWidth, (int)zoneHeight, (int)scale);
            var colMapping = new int[zone.Width * zone.ColorFormat.BytesPerPixel];
            var factor = (double)zone.Width / numLeds;
            for (int i = 0; i < zone.Width; i++)
            {
                var ledNbr = (int)(i / factor);
                switch (zone.ColorFormat)
                {
                    case ColorFormatABGR:
                        colMapping[i * zone.ColorFormat.BytesPerPixel] = -1;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 1] = ledNbr * 3 + 2;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 2] = ledNbr * 3 + 1;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 3] = ledNbr + 3;
                        break;
                    case ColorFormatARGB:
                        colMapping[i * zone.ColorFormat.BytesPerPixel] = -1;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 1] = ledNbr * 3;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 2] = ledNbr * 3 + 1;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 3] = ledNbr * 3 + 2;
                        break;
                    case ColorFormatBGR:
                        colMapping[i * zone.ColorFormat.BytesPerPixel] = ledNbr * 3 + 2;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 1] = ledNbr * 3 + 1;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 2] = ledNbr * 3;
                        break;
                    case ColorFormatBGRA:
                        colMapping[i * zone.ColorFormat.BytesPerPixel] = ledNbr * 3 + 2;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 1] = ledNbr * 3 + 1;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 2] = ledNbr * 3;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 3] = -1;
                        break;
                    case ColorFormatRGB:
                        colMapping[i * zone.ColorFormat.BytesPerPixel] = ledNbr * 3;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 1] = ledNbr * 3 + 1;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 2] = ledNbr * 3 + 2;
                        break;
                    case ColorFormatRGBA:
                        colMapping[i * zone.ColorFormat.BytesPerPixel] = ledNbr * 3;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 1] = ledNbr * 3 + 1;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 2] = ledNbr * 3 + 2;
                        colMapping[i * zone.ColorFormat.BytesPerPixel + 3] = -1;
                        break;

                }
                rowPerLed[ledNbr]++;
            }

            var rowLength = zone.Width * 4;
            var token = tokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                screenCapture.CaptureScreen();
                Array.Clear(sumBuffer);
                using (zone.Lock())
                {
                    var raw = zone.RawBuffer;
                    for (int i = 0; i < raw.Length; i++)
                    {
                        var col = i % rowLength;
                        var target = colMapping[col];
                        if (target > 0)
                        {
                            sumBuffer[target] += raw[i];
                        }
                    }
                }

                for (int i = 0; i < numLeds; i++)
                {
                    var mapped = flip ? end - 1 - i : i + start;
                    var numValues = (ulong)(zone.Height * rowPerLed[i]);
                    var red = (byte)(sumBuffer[i * 3] / numValues);
                    var green = (byte)(sumBuffer[i * 3 + 1] / numValues);
                    var blue = (byte)(sumBuffer[i * 3 + 2] / numValues);
                    var maxValue = red;
                    if (green > maxValue)
                    {
                        maxValue = green;
                    }

                    if (blue > maxValue)
                    {
                        maxValue = blue;
                    }

                    var b = brightness;
                    if (maxValue < 50)
                    {
                        b /= 4;
                    }
                    else if (maxValue < 100)
                    {
                        b /= 2;
                    }

                    sendBuffer[mapped * 4] = red;
                    sendBuffer[mapped * 4 + 1] = green;
                    sendBuffer[mapped * 4 + 2] = blue;
                    sendBuffer[mapped * 4 + 3] = b;
                }

                comm.SetRGBB(sendBuffer, false);

                //await Task.Delay(50);
            }

            screenCapture.UnregisterCaptureZone(zone);
            zone = null;
            tokenSource = null;
        });
    }

    public void Stop()
    {
        tokenSource?.Cancel();
    }
}