using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using DataPersistance.Models;
using DataPersistance.Modules;
using Newtonsoft.Json;

namespace Ambilight.ViewModels;

public partial class MainViewModel : ViewModelBase, INotifyPropertyChanged
{
    private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiHome", "ambilight-user_settings.json");

    private ScreenReader.ScreenReader reader = new();
    private ModuleRepository repo = new(new PiHomeContext());

    private (string Name, int DeviceId) selectedCard;
    private ObservableCollection<(string Name, int Index)> displays = new();
    private (string Name, int Index) selectedDisplay;
    private IImage screenPreview = new WriteableBitmap(new PixelSize(100, 100), new Vector(200, 200), PixelFormat.Rgb32, AlphaFormat.Unpremul);
    private Module? selectedModule;
    private double xArea;
    private double yArea;
    private double areaWidth;
    private double areaHeight;

    public string Greeting => "Enable Ambilght!";
    public IList<(string Name, int DeviceId)> GraphicCards => reader.GraphicCards.ToList();
    public IList<(string Name, int Id)> Modules => repo.GetAllModules().Select(x => (x.Name, x.Id)).ToList();
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public int Brightness { get; set; }
    public bool Flip { get; set; }
    public bool CanRefresh { get; private set; }
    public bool ShowArea { get; private set; }
    public bool CanStart => selectedModule is not null && ShowArea && !CanStop;
    public bool CanStop { get; private set; }
    public bool Autostart { get; private set; }
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

    public ICommand SaveCommand => new RelayCommand(() =>
    {
        var settings = new PersistentSettings
        {
            SelectedCard = selectedCard.DeviceId,
            SelectedDisplay = selectedDisplay.Index,
            SelectedModule = selectedModule!.Id,
            Start = StartIndex,
            End = EndIndex,
            Brightness = Brightness,
            Flip = Flip,
            X = xArea,
            Y = yArea,
            Width = areaWidth,
            Height = areaHeight,
            Autostart = Autostart
        };
        var serialized = JsonConvert.SerializeObject(settings);
        using var fs = new FileStream(settingsPath, FileMode.Create, FileAccess.Write);
        fs.Write(Encoding.UTF32.GetBytes(serialized));
        fs.Flush();
        fs.Close();
    });

    public MainViewModel()
    {
        ReadSettings();
    }

    public void AreaSet(double x, double y, double width, double height)
    {
        xArea = x;
        yArea = y;
        areaWidth = width;
        areaHeight = height;
        reader.SetArea(x, y, width, height);
        ShowArea = true;
        OnPropertyChanged(nameof(ShowArea));
        OnPropertyChanged(nameof(CanStart));
    }

    private void ReadSettings()
    {
        if (!File.Exists(settingsPath))
        {
            return;
        }
        using var fs = new FileStream(settingsPath, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(fs, Encoding.UTF32);
        var buffer = reader.ReadToEnd();
        fs.Flush();
        fs.Close();
        var settings = JsonConvert.DeserializeObject<PersistentSettings>(buffer);
        if (settings == null)
        {
            return;
        }
        var card = GraphicCards.SingleOrDefault(x => x.DeviceId == settings.SelectedCard);
        if (card != default)
        {
            SelectedCard = card;
            var display = Displays.SingleOrDefault(x => x.Index == settings.SelectedDisplay);
            if (display != default)
            {
                SelectedDisplay = display;
            }
        }

        var module = Modules.SingleOrDefault(x => x.Id == settings.SelectedModule);
        if (module != default)
        {
            SelectedModule = module;
        }

        StartIndex = settings.Start;
        EndIndex = settings.End;
        Brightness = settings.Brightness;
        Flip = settings.Flip;
        xArea = settings.X;
        yArea = settings.Y;
        areaHeight = settings.Height;
        areaWidth = settings.Width;
        Autostart = settings.Autostart;
        reader.SetArea(xArea, yArea, areaWidth, areaHeight);
        OnPropertyChanged(nameof(ShowArea));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(StartIndex));
        OnPropertyChanged(nameof(EndIndex));
        OnPropertyChanged(nameof(Brightness));
        OnPropertyChanged(nameof(Flip));
        ShowArea = true;
        if (Autostart)
        {
            StartCommand.Execute(null);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    unsafe T* PointerTo<T>(ReadOnlySpan<T> from) where T : struct => *(T**)&from;

    private class PersistentSettings
    {
        public int SelectedCard { get; set; }
        public int SelectedDisplay { get; set; }
        public int SelectedModule { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public int Brightness { get; set; }
        public bool Flip { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool Autostart { get; set; }
    }
}
