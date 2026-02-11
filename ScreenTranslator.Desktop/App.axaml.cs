using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services;
using ScreenTranslator.Core.Services.Interfaces;
using ScreenTranslator.Core.ViewModels;
using ScreenTranslator.Desktop.Views;

namespace ScreenTranslator.Desktop;

public partial class App : Application
{
    private SettingsService _settingsService = null!;
    private IHotkeyService _hotkeyService = null!;
    private IMonitorService _monitorService = null!;
    private IMouseHookService _mouseHookService = null!;
    private IScreenCaptureService _captureService = null!;
    private IPlatformService _platformService = null!;
    private AiTranslateService _aiService = null!;

    private AppSettings _settings = null!;
    private MainViewModel _mainViewModel = null!;
    private MainWindow _mainWindow = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            InitializeServices();

            if (!_platformService.EnsureSingleInstance())
            {
                desktop.Shutdown();
                return;
            }

            _mainViewModel = new MainViewModel
            {
                FontSize = _settings.UI.FontSize,
                ShowOriginalText = _settings.UI.ShowOriginalText
            };
            _mainWindow = new MainWindow(_mainViewModel);
            _mainWindow.SettingsRequested += OnSettingsRequested;
            _mainWindow.Closing += (_, args) =>
            {
                args.Cancel = true;
                _mainWindow.Hide();
            };

            desktop.MainWindow = _mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
            if (!_hotkeyService.Register(_settings.Hotkey))
            {
                // Hotkey registration failed - user will need to change in settings
            }

            _mainWindow.Show();
            PositionMainWindow();
            ApplyTheme(_settings.UI.Theme);

            if (_settings.UI.WindowMode == WindowMode.Sidebar)
                _mainWindow.ApplySidebarMode(_settings.UI.SidebarPosition, _settings.UI.PreferredMonitor, _monitorService, _settings.UI.Opacity);
            else
                _mainWindow.Opacity = _settings.UI.Opacity;

            _mainViewModel.SetStatus("就绪 - 按 " + _settings.Hotkey + " 开始翻译");

            desktop.ShutdownRequested += (_, _) => Cleanup();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitializeServices()
    {
        if (OperatingSystem.IsWindows())
        {
            _platformService = new Platform.Windows.WindowsPlatformService();
            _monitorService = new Platform.Windows.WindowsMonitorService();
            _captureService = new Platform.Windows.WindowsScreenCaptureService();
            _hotkeyService = new Platform.Windows.WindowsHotkeyService();
            _mouseHookService = new Platform.Windows.WindowsMouseHookService();
        }
        else if (OperatingSystem.IsMacOS())
        {
            _platformService = new Platform.macOS.MacPlatformService();
            _monitorService = new Platform.macOS.MacMonitorService();
            _captureService = new Platform.macOS.MacScreenCaptureService();
            _hotkeyService = new Platform.macOS.MacHotkeyService();
            _mouseHookService = new Platform.macOS.MacMouseHookService();
        }
        else
        {
            throw new PlatformNotSupportedException("Only Windows and macOS are supported.");
        }

        _hotkeyService.SetMouseHookService(_mouseHookService);
        _settingsService = new SettingsService(_platformService);
        _settings = _settingsService.Load();
        _aiService = new AiTranslateService();
    }

    private void PositionMainWindow()
    {
        var settings = _settings.UI;
        if (!double.IsNaN(settings.WindowLeft) && !double.IsNaN(settings.WindowTop))
        {
            _mainWindow.Position = new Avalonia.PixelPoint((int)settings.WindowLeft, (int)settings.WindowTop);
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

        await overlay.ShowDialog(_mainWindow);

        if (!overlay.ViewModel.Confirmed || overlay.ViewModel.SelectedRegion == null)
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
            var imageData = _captureService.CaptureRegion(region);
            _mainViewModel.CapturedImageData = imageData;

            _mainViewModel.SetStatus("正在 AI 截图翻译...");
            var result = await _aiService.TranslateImageAsync(
                imageData, _settings.TargetLanguage, _settings.AI);
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

        var vm = new SettingsViewModel(_settingsService, _monitorService);
        var win = new SettingsWindow(vm);
        win.ShowDialog(_mainWindow).ContinueWith(_ =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (win.Saved)
                {
                    _settings = _settingsService.Load();
                    _mainViewModel.FontSize = _settings.UI.FontSize;
                    _hotkeyService.Register(_settings.Hotkey);
                    _mainViewModel.SetStatus("就绪 - 按 " + _settings.Hotkey + " 开始翻译");

                    if (_settings.UI.PreferredMonitor != previousMonitor)
                    {
                        _settings.UI.WindowLeft = double.NaN;
                        _settings.UI.WindowTop = double.NaN;
                        _settingsService.Save(_settings);
                        var monitor = _monitorService.GetMonitorByIndex(_settings.UI.PreferredMonitor);
                        if (monitor != null)
                            _mainWindow.PositionOnMonitor(monitor.WorkingArea);
                    }

                    if (_settings.UI.WindowMode == WindowMode.Sidebar)
                        _mainWindow.ApplySidebarMode(_settings.UI.SidebarPosition, _settings.UI.PreferredMonitor, _monitorService, _settings.UI.Opacity);
                    else
                        _mainWindow.RestoreIndependentMode(_settings.UI);

                    ApplyTheme(_settings.UI.Theme);
                    _platformService.SetAutoStart(_settings.UI.AutoStart);
                }
            });
        });
    }

    private void ApplyTheme(AppTheme theme)
    {
        RequestedThemeVariant = theme == AppTheme.Light ? ThemeVariant.Light : ThemeVariant.Dark;

        var themeFile = theme == AppTheme.Light
            ? "avares://ScreenTranslator.Desktop/Resources/LightTheme.axaml"
            : "avares://ScreenTranslator.Desktop/Resources/DarkTheme.axaml";

        var mergedDicts = Resources.MergedDictionaries;
        mergedDicts.Clear();
        mergedDicts.Add(new Avalonia.Markup.Xaml.Styling.ResourceInclude(new Uri(themeFile)));

        var accentHex = _settings.UI.AccentColor;
        if (!string.IsNullOrWhiteSpace(accentHex))
        {
            try
            {
                var color = Color.Parse(accentHex);
                var dark = Color.FromRgb(
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
    }

    private void Cleanup()
    {
        if (_mainWindow != null)
        {
            var bounds = _mainWindow.Bounds;
            var pos = _mainWindow.Position;
            _settings.UI.WindowLeft = pos.X;
            _settings.UI.WindowTop = pos.Y;
            _settings.UI.WindowWidth = bounds.Width;
            _settings.UI.WindowHeight = bounds.Height;
            _settings.UI.ShowOriginalText = _mainViewModel.ShowOriginalText;
            _settingsService.Save(_settings);
        }

        _hotkeyService?.Dispose();
        _mouseHookService?.Dispose();
        _aiService?.Dispose();
        _platformService?.ReleaseSingleInstance();
    }

    public void ExitApp()
    {
        Cleanup();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
