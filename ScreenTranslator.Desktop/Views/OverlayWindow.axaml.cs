using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.ViewModels;

namespace ScreenTranslator.Desktop.Views;

public partial class OverlayWindow : Window
{
    public OverlayViewModel ViewModel { get; }

    public OverlayWindow() : this(new RectInfo()) { }

    public OverlayWindow(RectInfo virtualBounds)
    {
        InitializeComponent();
        ViewModel = new OverlayViewModel();
        DataContext = ViewModel;

        // Position to cover virtual screen
        Position = new PixelPoint(virtualBounds.X, virtualBounds.Y);
        Width = virtualBounds.Width;
        Height = virtualBounds.Height;

        // DPI: Avalonia uses device-independent pixels by default
        // For overlay we use 1:1 since we position in physical pixels
        ViewModel.DpiX = 1.0;
        ViewModel.DpiY = 1.0;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                ViewModel.Cancel();
                Close();
            }
        };
    }

    private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(OverlayCanvas);
        ViewModel.StartSelection(pos.X, pos.Y);
        SelectionRect.IsVisible = true;
        e.Pointer.Capture(OverlayCanvas);
        UpdateSelectionRect();
    }

    private void Canvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!ViewModel.IsSelecting) return;
        var pos = e.GetPosition(OverlayCanvas);
        ViewModel.UpdateSelection(pos.X, pos.Y);
        UpdateSelectionRect();
    }

    private void Canvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!ViewModel.IsSelecting) return;
        e.Pointer.Capture(null);

        var pos = e.GetPosition(OverlayCanvas);
        ViewModel.EndSelection(pos.X, pos.Y);

        if (ViewModel.Confirmed)
        {
            Close();
        }
        else
        {
            SelectionRect.IsVisible = false;
        }
    }

    private void UpdateSelectionRect()
    {
        Canvas.SetLeft(SelectionRect, ViewModel.SelectionX);
        Canvas.SetTop(SelectionRect, ViewModel.SelectionY);
        SelectionRect.Width = Math.Max(0, ViewModel.SelectionWidth);
        SelectionRect.Height = Math.Max(0, ViewModel.SelectionHeight);
    }
}
