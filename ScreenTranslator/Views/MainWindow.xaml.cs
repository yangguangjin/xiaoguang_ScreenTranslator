using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ScreenTranslator.Helpers;
using ScreenTranslator.Models;
using ScreenTranslator.Services;
using ScreenTranslator.ViewModels;

namespace ScreenTranslator.Views;

public partial class MainWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public MainViewModel ViewModel { get; }

    public bool IsLogPanelCollapsed
    {
        get => LogContentPanel.Visibility == Visibility.Collapsed;
        set
        {
            LogContentPanel.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
            LogToggleButton.Content = value ? "▸" : "▾";
        }
    }

    public event EventHandler? SettingsRequested;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.ShowOriginalText)
        {
            OriginalColumn.Width = new GridLength(0, GridUnitType.Star);
            SplitterColumn.Width = new GridLength(0);
            OriginalTextBorder.Visibility = Visibility.Collapsed;
            ColumnSplitter.Visibility = Visibility.Collapsed;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ShowOriginalText))
            AnimateOriginalTextPanel(ViewModel.ShowOriginalText);
    }

    private void AnimateOriginalTextPanel(bool show)
    {
        var duration = new Duration(TimeSpan.FromMilliseconds(300));

        if (show)
        {
            OriginalTextBorder.Visibility = Visibility.Visible;
            ColumnSplitter.Visibility = Visibility.Visible;

            var colAnim = new GridLengthAnimation
            {
                From = new GridLength(0, GridUnitType.Star),
                To = new GridLength(1, GridUnitType.Star),
                Duration = duration
            };
            var splitterAnim = new GridLengthAnimation
            {
                From = new GridLength(0),
                To = new GridLength(5),
                Duration = duration
            };

            OriginalColumn.BeginAnimation(ColumnDefinition.WidthProperty, colAnim);
            SplitterColumn.BeginAnimation(ColumnDefinition.WidthProperty, splitterAnim);
        }
        else
        {
            var colAnim = new GridLengthAnimation
            {
                From = new GridLength(1, GridUnitType.Star),
                To = new GridLength(0, GridUnitType.Star),
                Duration = duration
            };
            var splitterAnim = new GridLengthAnimation
            {
                From = new GridLength(5),
                To = new GridLength(0),
                Duration = duration
            };

            colAnim.Completed += (_, _) =>
            {
                OriginalTextBorder.Visibility = Visibility.Collapsed;
                ColumnSplitter.Visibility = Visibility.Collapsed;
            };

            OriginalColumn.BeginAnimation(ColumnDefinition.WidthProperty, colAnim);
            SplitterColumn.BeginAnimation(ColumnDefinition.WidthProperty, splitterAnim);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            if (ViewModel.WindowMode != WindowMode.Sidebar)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal : WindowState.Maximized;
            }
        }
        else
        {
            if (ViewModel.WindowMode != WindowMode.Sidebar)
                DragMove();
        }
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
        => Hide();

    private void SettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsToolbarPopupOpen = false;
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
        => ViewModel.IsToolbarPopupOpen = !ViewModel.IsToolbarPopupOpen;

    private void ClearApiLog_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ApiLog = string.Empty;
        ViewModel.HasError = false;
        ViewModel.ErrorDetail = string.Empty;
    }

    private void ToggleLogPanel_Click(object sender, RoutedEventArgs e)
    {
        if (LogContentPanel.Visibility == Visibility.Visible)
        {
            LogContentPanel.Visibility = Visibility.Collapsed;
            LogToggleButton.Content = "▸";
        }
        else
        {
            LogContentPanel.Visibility = Visibility.Visible;
            LogToggleButton.Content = "▾";
        }
    }

    private void ClickThrough_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        if (ViewModel.IsClickThrough)
            SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT);
        else
            SetWindowLong(hwnd, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
    }

    public void DisableClickThrough()
    {
        ViewModel.IsClickThrough = false;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
    }

    public void PositionOnMonitor(System.Drawing.Rectangle workingArea)
    {
        var (dpiX, dpiY) = Helpers.DpiHelper.GetDpi(this);
        if (dpiX == 0) dpiX = 1;
        if (dpiY == 0) dpiY = 1;

        Left = workingArea.X / dpiX;
        Top = workingArea.Y / dpiY;
        Width = Math.Min(Width, workingArea.Width / dpiX);
        Height = Math.Min(Height, workingArea.Height / dpiY);
    }

    public void ApplySidebarMode(SidebarPosition position, int monitorIndex, MonitorService monitorService, double opacity = 0.85)
    {
        var monitor = monitorService.GetMonitorByIndex(monitorIndex);
        System.Drawing.Rectangle wa;
        if (monitor != null)
            wa = monitor.WorkingArea;
        else
        {
            var screen = System.Windows.Forms.Screen.PrimaryScreen!;
            wa = screen.WorkingArea;
        }

        var (dpiX, dpiY) = Helpers.DpiHelper.GetDpi(this);
        if (dpiX == 0) dpiX = 1;
        if (dpiY == 0) dpiY = 1;

        // wa is in physical pixels, convert to WPF units
        double waLeft = wa.Left / dpiX;
        double waTop = wa.Top / dpiY;
        double waWidth = wa.Width / dpiX;
        double waHeight = wa.Height / dpiY;

        const double sidebarWidth = 400;
        const double sidebarHeight = 300;

        switch (position)
        {
            case SidebarPosition.Right:
                Width = sidebarWidth;
                Height = waHeight;
                Left = waLeft + waWidth - sidebarWidth;
                Top = waTop;
                break;
            case SidebarPosition.Left:
                Width = sidebarWidth;
                Height = waHeight;
                Left = waLeft;
                Top = waTop;
                break;
            case SidebarPosition.Top:
                Width = waWidth;
                Height = sidebarHeight;
                Left = waLeft;
                Top = waTop;
                break;
            case SidebarPosition.Bottom:
                Width = waWidth;
                Height = sidebarHeight;
                Left = waLeft;
                Top = waTop + waHeight - sidebarHeight;
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
            Left = ui.WindowLeft;
            Top = ui.WindowTop;
        }
        else
        {
            // Center on current screen
            var screen = System.Windows.Forms.Screen.FromHandle(
                new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var wa = screen.WorkingArea;
            var (dpiX, dpiY) = Helpers.DpiHelper.GetDpi(this);
            if (dpiX == 0) dpiX = 1;
            if (dpiY == 0) dpiY = 1;
            Left = (wa.Left + (wa.Width - Width * dpiX) / 2) / dpiX;
            Top = (wa.Top + (wa.Height - Height * dpiY) / 2) / dpiY;
        }
    }

    public void ApplyBackgroundImage(string path, double opacity)
    {
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
        {
            BackgroundImage.Visibility = Visibility.Collapsed;
            BackgroundImage.Source = null;
            // Remove window-level override so theme default SurfaceBrush is used
            Resources.Remove("SurfaceBrush");
            return;
        }

        try
        {
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            BackgroundImage.Source = bitmap;
            BackgroundImage.Opacity = opacity;
            BackgroundImage.Visibility = Visibility.Visible;

            // Make SurfaceBrush semi-transparent at window level so image shows through
            var surfaceBrush = (System.Windows.Media.SolidColorBrush)FindResource("SurfaceBrush");
            var c = surfaceBrush.Color;
            Resources["SurfaceBrush"] = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(200, c.R, c.G, c.B));
        }
        catch
        {
            BackgroundImage.Visibility = Visibility.Collapsed;
            BackgroundImage.Source = null;
            Resources.Remove("SurfaceBrush");
        }
    }
}
