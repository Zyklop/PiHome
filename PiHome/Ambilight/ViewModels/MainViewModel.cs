using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;

namespace Ambilight.ViewModels;

public partial class MainViewModel : ViewModelBase, INotifyPropertyChanged
{
    public ScreenReader.ScreenReader reader = new();
    private (string Name, int DeviceId) selectedCard;
    private ObservableCollection<(string Name, int Index)> displays = new();
    private (string Name, int Index) selectedDisplay;
    private IImage screenPreview = new WriteableBitmap(new PixelSize(100, 100), new Vector(200, 200), PixelFormat.Rgb32, AlphaFormat.Unpremul);

    public string Greeting => "Enable Ambilght!";
    public IList<(string Name, int DeviceId)> GraphicCards => reader.GraphicCards.ToList();

    public (string Name, int DeviceId) SelectedCard
    {
        get { return selectedCard; }
        set
        {
            reader.SetGraphicCard(value.DeviceId);
            selectedCard = value;
            displays.Clear();
            CanRefresh = false;
            OnPropertyChanged(nameof(CanRefresh));
            ShowArea = false;
            OnPropertyChanged(nameof(ShowArea));
            foreach ((string Name, int Index) display in reader.Displays)
            {
                displays.Add(display);
            }
        }
    }

    public ObservableCollection<(string Name, int Index)> Displays => displays;
    public (string Name, int Index) SelectedDisplay
    {
        get { return selectedDisplay; }
        set
        {
            reader.SetDisplay(value.Index);
            selectedDisplay = value;
            CanRefresh = true;
            OnPropertyChanged(nameof(CanRefresh));
            ShowArea = false;
            OnPropertyChanged(nameof(ShowArea));
        }
    }

    public bool CanRefresh { get; private set; }
    public bool ShowArea { get; private set; }

    public ICommand RefreshCommand => new RelayCommand(() =>
    {
        unsafe
        {
            var image = reader.GetFullscreenImage2(out var width, out var height);
            var wbp = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            using var target = wbp.Lock();
            Buffer.MemoryCopy(PointerTo(image), (byte*)target.Address, image.Length, image.Length);
            screenPreview = wbp;
            OnPropertyChanged(nameof(ScreenPreview));
        }
    });

    public IImage ScreenPreview => screenPreview;

    public void AreaSet(double x, double y, double width, double height)
    {
        reader.SetArea(x, y, width, height);
        ShowArea = true;
        OnPropertyChanged(nameof(ShowArea));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    unsafe T* PointerTo<T>(ReadOnlySpan<T> from) where T : struct => *(T**)&from;
}
