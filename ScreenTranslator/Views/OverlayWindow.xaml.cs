using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenTranslator.Helpers;
using ScreenTranslator.ViewModels;

namespace ScreenTranslator.Views;

public partial class OverlayWindow : Window
{
    public OverlayViewModel ViewModel { get; }

    public OverlayWindow(System.Drawing.Rectangle virtualBounds)
    {
        InitializeComponent();
        ViewModel = new OverlayViewModel();
        DataContext = ViewModel;

        Loaded += (_, _) =>
        {
            var (dpiX, dpiY) = DpiHelper.GetDpi(this);
            ViewModel.DpiX = dpiX;
            ViewModel.DpiY = dpiY;

            var wpfBounds = DpiHelper.PhysicalToWpf(virtualBounds, dpiX, dpiY);
            Left = wpfBounds.X;
            Top = wpfBounds.Y;
            Width = wpfBounds.Width;
            Height = wpfBounds.Height;
        };

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                ViewModel.Cancel();
                DialogResult = false;
                Close();
            }
        };
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(OverlayCanvas);
        ViewModel.StartSelection(pos.X, pos.Y);
        SelectionRect.Visibility = Visibility.Visible;
        OverlayCanvas.CaptureMouse();
        UpdateSelectionRect();
    }

    private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!ViewModel.IsSelecting) return;
        var pos = e.GetPosition(OverlayCanvas);
        ViewModel.UpdateSelection(pos.X, pos.Y);
        UpdateSelectionRect();
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!ViewModel.IsSelecting) return;
        OverlayCanvas.ReleaseMouseCapture();

        var pos = e.GetPosition(OverlayCanvas);
        ViewModel.EndSelection(pos.X, pos.Y);

        if (ViewModel.Confirmed)
        {
            DialogResult = true;
            Close();
        }
        else
        {
            SelectionRect.Visibility = Visibility.Collapsed;
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
