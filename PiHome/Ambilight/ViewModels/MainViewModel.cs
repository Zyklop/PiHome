using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using DataPersistance.Models;
using DataPersistance.Modules;

namespace Ambilight.ViewModels;

public partial class MainViewModel : ViewModelBase, INotifyPropertyChanged
{
    private ScreenReader.ScreenReader reader = new();
    private ModuleRepository repo = new ModuleRepository(new PiHomeContext());

    private (string Name, int DeviceId) selectedCard;
    private ObservableCollection<(string Name, int Index)> displays = new();
    private (string Name, int Index) selectedDisplay;
    private IImage screenPreview = new WriteableBitmap(new PixelSize(100, 100), new Vector(200, 200), PixelFormat.Rgb32, AlphaFormat.Unpremul);
    private Module? selectedModule;

    public string Greeting => "Enable Ambilght!";
    public IList<(string Name, int DeviceId)> GraphicCards => reader.GraphicCards.ToList();
    public IList<(string Name, int Ip)> Modules => repo.GetAllModules().Select(x => (x.Name, x.Id)).ToList();
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public int Brightness { get; set; }
    public bool Flip { get; set; }
    public bool CanRefresh { get; private set; }
    public bool ShowArea { get; private set; }
    public bool CanStart => selectedModule is not null && ShowArea && !CanStop;
    public bool CanStop { get; private set; }
    public IImage ScreenPreview => screenPreview;

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
    public (string Name, int Id)? SelectedModule
    {
        get { return selectedModule is null ? null : (selectedModule.Name, selectedModule.Id); }
        set
        {
            var module = repo.GetModule(value.Value.Id);
            selectedModule = module;
            StartIndex = module.Leds.Min(x => x.Index);
            EndIndex = module.Leds.Max(x => x.Index);
            OnPropertyChanged(nameof(StartIndex));
            OnPropertyChanged(nameof(EndIndex));
            OnPropertyChanged(nameof(CanStart));
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

    public ICommand StartCommand => new RelayCommand(() =>
    {
        reader.Start(selectedModule.Ip, StartIndex, EndIndex, (byte)Brightness, Flip);
        CanStop = true;
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
    });

    public ICommand StopCommand => new RelayCommand(() =>
    {
        reader.Stop();
        CanStop = false;
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
    });

    public void AreaSet(double x, double y, double width, double height)
    {
        reader.SetArea(x, y, width, height);
        ShowArea = true;
        OnPropertyChanged(nameof(ShowArea));
        OnPropertyChanged(nameof(CanStart));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    unsafe T* PointerTo<T>(ReadOnlySpan<T> from) where T : struct => *(T**)&from;
}
