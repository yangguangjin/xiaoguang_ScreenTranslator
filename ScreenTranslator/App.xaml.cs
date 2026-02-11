using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using ScreenTranslator.Models;
using ScreenTranslator.Services;
using ScreenTranslator.ViewModels;
using ScreenTranslator.Views;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ScreenTranslator;

public partial class App : Application
{
    private static Mutex? _mutex;

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private SettingsService _settingsService = null!;
    private HotkeyService _hotkeyService = null!;
    private MonitorService _monitorService = null!;
    private MouseHookService _mouseHookService = null!;
    private ScreenCaptureService _captureService = null!;
    private AiTranslateService _aiService = null!;

    private AppSettings _settings = null!;
    private MainViewModel _mainViewModel = null!;
    private MainWindow _mainWindow = null!;
    private TaskbarIcon? _trayIcon;
    private System.Windows.Controls.MenuItem? _translateMenuItem;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single instance
        _mutex = new Mutex(true, "ScreenTranslator_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("小光翻译已在运行中。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Global exception handler
        DispatcherUnhandledException += OnUnhandledException;

        // Init services
        _settingsService = new SettingsService();
        _settings = _settingsService.Load();
        _monitorService = new MonitorService();
        _captureService = new ScreenCaptureService();
        _aiService = new AiTranslateService();
        _hotkeyService = new HotkeyService();
        _mouseHookService = new MouseHookService();
        _hotkeyService.SetMouseHookService(_mouseHookService);

        // Init main window
        _mainViewModel = new MainViewModel
        {
            FontSize = _settings.UI.FontSize,
            ShowOriginalText = _settings.UI.ShowOriginalText
        };
        _mainWindow = new MainWindow(_mainViewModel);
        _mainWindow.SettingsRequested += OnSettingsRequested;
        _mainWindow.Closing += (_, args) => { args.Cancel = true; _mainWindow.Hide(); };
        _mainWindow.Loaded += (_, _) => _mainWindow.IsLogPanelCollapsed = _settings.UI.IsLogPanelCollapsed;

        // Setup tray icon
        SetupTrayIcon();

        // Register hotkey
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        if (!_hotkeyService.Register(_settings.Hotkey))
        {
            MessageBox.Show($"无法注册快捷键: {_settings.Hotkey}\n请在设置中修改。",
                "快捷键注册失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Init OCR engine lazily (on first use)
        _mainWindow.Show();
        PositionMainWindow();

        // Apply initial theme
        ApplyTheme(_settings.UI.Theme);

        // Apply sidebar mode if configured
        if (_settings.UI.WindowMode == WindowMode.Sidebar)
            _mainWindow.ApplySidebarMode(_settings.UI.SidebarPosition, _settings.UI.PreferredMonitor, _monitorService, _settings.UI.Opacity);
        else
            _mainWindow.Opacity = _settings.UI.Opacity;

        _mainViewModel.SetStatus("就绪 - 按 " + _settings.Hotkey + " 开始翻译");
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            IconSource = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/icon.ico")),
            ToolTipText = "小光翻译",
            MenuActivation = PopupActivationMode.RightClick,
            ContextMenu = new System.Windows.Controls.ContextMenu()
        };

        var showItem = new System.Windows.Controls.MenuItem { Header = "显示窗口" };
        showItem.Click += (_, _) => { _mainWindow.Show(); _mainWindow.Activate(); };

        var translateItem = new System.Windows.Controls.MenuItem { Header = "翻译 (" + _settings.Hotkey + ")" };
        translateItem.Click += (_, _) => OnHotkeyPressed(this, EventArgs.Empty);
        _translateMenuItem = translateItem;

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "设置" };
        settingsItem.Click += (_, _) => OnSettingsRequested(this, EventArgs.Empty);

        var disableClickThroughItem = new System.Windows.Controls.MenuItem { Header = "取消穿透" };
        disableClickThroughItem.Click += (_, _) => _mainWindow.DisableClickThrough();

        var exitItem = new System.Windows.Controls.MenuItem { Header = "退出" };
        exitItem.Click += (_, _) => ExitApp();

        _trayIcon.ContextMenu.Items.Add(showItem);
        _trayIcon.ContextMenu.Items.Add(translateItem);
        _trayIcon.ContextMenu.Items.Add(new System.Windows.Controls.Separator());
        _trayIcon.ContextMenu.Items.Add(settingsItem);
        _trayIcon.ContextMenu.Items.Add(disableClickThroughItem);
        _trayIcon.ContextMenu.Items.Add(new System.Windows.Controls.Separator());
        _trayIcon.ContextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu.Opened += (_, _) =>
        {
            disableClickThroughItem.Visibility = _mainViewModel.IsClickThrough
                ? Visibility.Visible : Visibility.Collapsed;
        };

        _trayIcon.TrayMouseDoubleClick += (_, _) => { _mainWindow.Show(); _mainWindow.Activate(); };
    }

    private void PositionMainWindow()
    {
        var settings = _settings.UI;
        if (!double.IsNaN(settings.WindowLeft) && !double.IsNaN(settings.WindowTop))
        {
            _mainWindow.Left = settings.WindowLeft;
            _mainWindow.Top = settings.WindowTop;
            _mainWindow.Width = settings.WindowWidth;
            _mainWindow.Height = settings.WindowHeight;
            return;
        }

        var monitor = _monitorService.GetMonitorByIndex(settings.PreferredMonitor)
                      ?? _monitorService.GetSecondaryMonitor();
        if (monitor != null)
            _mainWindow.PositionOnMonitor(monitor.WorkingArea);
    }

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        var virtualBounds = _monitorService.GetVirtualScreenBounds();
        var overlay = new OverlayWindow(virtualBounds);
        var result = overlay.ShowDialog();

        if (result != true || overlay.ViewModel.SelectedRegion == null)
            return;

        var region = overlay.ViewModel.SelectedRegion;

        await PerformTranslation(region);
    }

    private async Task PerformTranslation(ScreenRegion region)
    {
        _mainWindow.Show();
        _mainWindow.Activate();
        _mainViewModel.IsTranslating = true;
        _mainViewModel.SetStatus("正在截屏...");

        try
        {
            using var bitmap = _captureService.CaptureRegion(region);

            _mainViewModel.CapturedImage = ConvertBitmapToBitmapSource(bitmap);

            _mainViewModel.SetStatus("正在 AI 截图翻译...");
            var result = await _aiService.TranslateImageAsync(
                bitmap, _settings.TargetLanguage, _settings.AI);
            _mainViewModel.UpdateResult(result);
        }
        catch (Exception ex)
        {
            _mainViewModel.SetStatus($"错误: {ex.Message}");
        }
        finally
        {
            _mainViewModel.IsTranslating = false;
        }
    }

    private void OnSettingsRequested(object? sender, EventArgs e)
    {
        var previousMonitor = _settings.UI.PreferredMonitor;

        // Detect which monitor the main window is currently on
        var hwnd = new System.Windows.Interop.WindowInteropHelper(_mainWindow).Handle;
        var currentScreen = System.Windows.Forms.Screen.FromHandle(hwnd);
        var allScreens = System.Windows.Forms.Screen.AllScreens;
        int currentMonitorIndex = Array.IndexOf(allScreens, currentScreen);
        if (currentMonitorIndex < 0) currentMonitorIndex = 0;

        var vm = new SettingsViewModel(_settingsService, _monitorService, currentMonitorIndex);
        var win = new SettingsWindow(vm) { Owner = _mainWindow };
        if (win.ShowDialog() == true)
        {
            _settings = _settingsService.Load();
            _mainViewModel.FontSize = _settings.UI.FontSize;

            _hotkeyService.Register(_settings.Hotkey);
            _mainViewModel.SetStatus("就绪 - 按 " + _settings.Hotkey + " 开始翻译");
            if (_translateMenuItem != null)
                _translateMenuItem.Header = "翻译 (" + _settings.Hotkey + ")";

            // Move window to new monitor if changed
            if (_settings.UI.PreferredMonitor != previousMonitor)
            {
                _settings.UI.WindowLeft = double.NaN;
                _settings.UI.WindowTop = double.NaN;
                _settingsService.Save(_settings);
                var monitor = _monitorService.GetMonitorByIndex(_settings.UI.PreferredMonitor);
                if (monitor != null)
                    _mainWindow.PositionOnMonitor(monitor.WorkingArea);
            }

            // Apply window mode
            if (_settings.UI.WindowMode == WindowMode.Sidebar)
            {
                _mainWindow.ApplySidebarMode(_settings.UI.SidebarPosition, _settings.UI.PreferredMonitor, _monitorService, _settings.UI.Opacity);
            }
            else
            {
                _mainWindow.RestoreIndependentMode(_settings.UI);
            }

            // Apply theme
            ApplyTheme(_settings.UI.Theme);

            // Apply auto-start
            ApplyAutoStart(_settings.UI.AutoStart);
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"发生未处理的异常:\n{e.Exception.Message}",
            "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void ApplyTheme(AppTheme theme)
    {
        var themeFile = theme == AppTheme.Light ? "LightTheme.xaml" : "DarkTheme.xaml";
        var mergedDicts = Resources.MergedDictionaries;
        mergedDicts.Clear();

        // Remove any top-level overrides from previous calls
        Resources.Remove("PrimaryBrush");
        Resources.Remove("PrimaryDarkBrush");

        mergedDicts.Add(new ResourceDictionary
            { Source = new Uri($"Resources/{themeFile}", UriKind.Relative) });
        mergedDicts.Add(new ResourceDictionary
            { Source = new Uri("Resources/Styles.xaml", UriKind.Relative) });

        // Apply custom accent color as a MergedDictionary (last dict wins)
        var accentHex = _settings.UI.AccentColor;
        if (!string.IsNullOrWhiteSpace(accentHex))
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(accentHex);
                var dark = System.Windows.Media.Color.FromRgb(
                    (byte)(color.R * 0.8), (byte)(color.G * 0.8), (byte)(color.B * 0.8));
                var accentDict = new ResourceDictionary
                {
                    ["PrimaryBrush"] = new SolidColorBrush(color),
                    ["PrimaryDarkBrush"] = new SolidColorBrush(dark)
                };
                mergedDicts.Add(accentDict);
            }
            catch { /* ignore invalid color */ }
        }

        // Apply background image
        _mainWindow?.ApplyBackgroundImage(_settings.UI.BackgroundImagePath, _settings.UI.BackgroundImageOpacity);
    }

    private void ApplyAutoStart(bool enable)
    {
        const string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        const string valueName = "ScreenTranslator";
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyName, true);
            if (key == null) return;
            if (enable)
            {
                var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                key.SetValue(valueName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(valueName, false);
            }
        }
        catch { /* ignore registry errors */ }
    }

    private static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
    {
        var hBitmap = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    private void ExitApp()
    {
        // Save window position
        if (_mainWindow.WindowState == WindowState.Normal)
        {
            _settings.UI.WindowLeft = _mainWindow.Left;
            _settings.UI.WindowTop = _mainWindow.Top;
            _settings.UI.WindowWidth = _mainWindow.Width;
            _settings.UI.WindowHeight = _mainWindow.Height;
        }
        _settings.UI.IsLogPanelCollapsed = _mainWindow.IsLogPanelCollapsed;
        _settings.UI.ShowOriginalText = _mainViewModel.ShowOriginalText;
        _settingsService.Save(_settings);

        _trayIcon?.Dispose();
        _hotkeyService.Dispose();
        _mouseHookService.Dispose();
        _aiService.Dispose();
        _mutex?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _hotkeyService?.Dispose();
        _mouseHookService?.Dispose();
        _aiService?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
