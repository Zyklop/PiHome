using System.Buffers;
using System.Globalization;
using ScreenCapture.NET.Pipewire.Connector;

namespace ScreenCapture.NET.Pipewire;

public sealed record PortalCaptureSession(string SessionHandle, uint StreamNode, int PipeWireFd);

public sealed class CapturedFrame : IDisposable
{
    private IMemoryOwner<byte>? _memoryOwner;
    private readonly byte[]? _directArray;

    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }
    public ReadOnlyMemory<byte> Data { get; }
    public SpaVideoFormat PixelFormat { get; }
    public int ByteLength { get; }

    public CapturedFrame(int width, int height, int stride, IMemoryOwner<byte> memoryOwner, int actualLength, SpaVideoFormat pixelFormat = SpaVideoFormat.BGRx)
    {
        Width = width;
        Height = height;
        Stride = stride;
        _memoryOwner = memoryOwner;
        Data = memoryOwner.Memory.Slice(0, actualLength);
        PixelFormat = pixelFormat;
        ByteLength = actualLength;
    }

    public CapturedFrame(int width, int height, int stride, byte[] data, SpaVideoFormat pixelFormat = SpaVideoFormat.BGRx)
    {
        Width = width;
        Height = height;
        Stride = stride;
        _directArray = data;
        Data = data;
        PixelFormat = pixelFormat;
        ByteLength = data.Length;
    }

    public void Dispose()
    {
        _memoryOwner?.Dispose();
        _memoryOwner = null;
    }
}
