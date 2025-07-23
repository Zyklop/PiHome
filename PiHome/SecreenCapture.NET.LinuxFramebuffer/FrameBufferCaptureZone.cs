using HPPH;
using ScreenCapture.NET;

namespace SecreenCapture.NET.LinuxFramebuffer;

public class FrameBufferCaptureZone : ICaptureZone
{
    internal readonly byte[] buffer;
    private readonly object _lock = new object();
    
    public FrameBufferCaptureZone(Display display, IColorFormat colorFormat, int x, int y, int width, int height, int stride)
    {
        Display = display;
        ColorFormat = colorFormat;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Stride = stride;
        buffer = new byte[width * height * colorFormat.BytesPerPixel];
    }

    public IDisposable Lock()
    {        
        Monitor.Enter(_lock);
        return new UnlockDisposable(_lock);
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
    public IColorFormat ColorFormat { get; }
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }
    public int DownscaleLevel => 0;
    public int UnscaledWidth => Width;
    public int UnscaledHeight => Height;
    public ReadOnlySpan<byte> RawBuffer => buffer;
    public IImage Image => throw new NotImplementedException();
    public bool AutoUpdate { get; set; }
    public bool IsUpdateRequested => false;
    public event EventHandler? Updated;
    
    private class UnlockDisposable : IDisposable
    {
        #region Properties & Fields

        private bool _disposed = false;
        private readonly object _lock;

        #endregion

        #region Constructors

        // ReSharper disable once ConvertToPrimaryConstructor
        public UnlockDisposable(object @lock) => this._lock = @lock;
        ~UnlockDisposable() => Dispose();

        #endregion

        #region Methods

        /// <inheritdoc />
        public void Dispose()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            Monitor.Exit(_lock);
            _disposed = true;

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}