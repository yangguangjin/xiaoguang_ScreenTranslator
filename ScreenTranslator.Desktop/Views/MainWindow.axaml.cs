using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;
using ScreenTranslator.Core.ViewModels;

namespace ScreenTranslator.Desktop.Views;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public event EventHandler? SettingsRequested;

    public MainWindow() : this(new MainViewModel()) { }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;

        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsTopmost))
                Topmost = ViewModel.IsTopmost;
        };
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (ViewModel.WindowMode != WindowMode.Sidebar)
                BeginMoveDrag(e);
        }
    }

    private void MinimizeBtn_Click(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseBtn_Click(object? sender, RoutedEventArgs e)
        => Hide();

    private void SettingsBtn_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.IsToolbarPopupOpen = false;
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void MenuButton_Click(object? sender, RoutedEventArgs e)
        => ViewModel.IsToolbarPopupOpen = !ViewModel.IsToolbarPopupOpen;
    private void ClearApiLog_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.ApiLog = string.Empty;
        ViewModel.HasError = false;
        ViewModel.ErrorDetail = string.Empty;
    }

    private void ToggleLogPanel_Click(object? sender, RoutedEventArgs e)
    {
        if (LogContentPanel.IsVisible)
        {
            LogContentPanel.IsVisible = false;
            LogToggleButton.Content = "▸";
        }
        else
        {
            LogContentPanel.IsVisible = true;
            LogToggleButton.Content = "▾";
        }
    }

    private void ClickThrough_Click(object? sender, RoutedEventArgs e)
    {
        // Click-through is platform-specific, handled via IPlatformService
        // The binding to IsClickThrough is already set
    }

    public void DisableClickThrough()
    {
        ViewModel.IsClickThrough = false;
    }

    public void PositionOnMonitor(RectInfo workingArea)
    {
        Position = new PixelPoint(workingArea.X, workingArea.Y);
        Width = Math.Min(Width, workingArea.Width);
        Height = Math.Min(Height, workingArea.Height);
    }

    public void ApplySidebarMode(SidebarPosition position, int monitorIndex, IMonitorService monitorService, double opacity = 0.85)
    {
        var monitor = monitorService.GetMonitorByIndex(monitorIndex);
        RectInfo wa;
        if (monitor != null)
            wa = monitor.WorkingArea;
        else
        {
            var monitors = monitorService.GetAllMonitors();
            wa = monitors.FirstOrDefault()?.WorkingArea ?? new RectInfo { Width = 1920, Height = 1080 };
        }

        const double sidebarWidth = 400;
        const double sidebarHeight = 300;

        switch (position)
        {
            case SidebarPosition.Right:
                Width = sidebarWidth;
                Height = wa.Height;
                Position = new PixelPoint(wa.X + wa.Width - (int)sidebarWidth, wa.Y);
                break;
            case SidebarPosition.Left:
                Width = sidebarWidth;
                Height = wa.Height;
                Position = new PixelPoint(wa.X, wa.Y);
                break;
            case SidebarPosition.Top:
                Width = wa.Width;
                Height = sidebarHeight;
                Position = new PixelPoint(wa.X, wa.Y);
                break;
            case SidebarPosition.Bottom:
                Width = wa.Width;
                Height = sidebarHeight;
                Position = new PixelPoint(wa.X, wa.Y + wa.Height - (int)sidebarHeight);
                break;
        }

        ViewModel.WindowMode = WindowMode.Sidebar;
        Opacity = opacity;
    }

    public void RestoreIndependentMode(UiSettings ui)
    {
        ViewModel.WindowMode = WindowMode.Independent;
        Opacity = ui.Opacity;

        Width = ui.WindowWidth;
        Height = ui.WindowHeight;
        if (!double.IsNaN(ui.WindowLeft) && !double.IsNaN(ui.WindowTop))
        {
            Position = new PixelPoint((int)ui.WindowLeft, (int)ui.WindowTop);
        }
    }
}
