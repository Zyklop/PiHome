using Ambilight.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Ambilight.Views;

public partial class MainView : UserControl
{
    private PointerPoint start;

    public MainView()
    {
        InitializeComponent();
    }

    public void DragStarted(object? sender, PointerPressedEventArgs args)
    {
        var img = sender as Control;
        start = args.GetCurrentPoint(img);
    }

    public void DragReleased(object? sender, PointerReleasedEventArgs args)
    {
        var img = sender as Control;
        var end = args.GetCurrentPoint(img);

        ((MainViewModel)DataContext).AreaSet(start.Position.X / img.Bounds.Width, start.Position.Y / img.Bounds.Height, (end.Position.X - start.Position.X) / img.Bounds.Width, (end.Position.Y - start.Position.Y) / img.Bounds.Height);
        rectArea.Margin = new Thickness(start.Position.X, start.Position.Y, img.Bounds.Right - end.Position.X, img.Bounds.Bottom - end.Position.Y);
    }
}
