using System.Windows;
using System.Windows.Input;
using ScreenTranslator.Helpers;
using ScreenTranslator.Models;
using ScreenTranslator.ViewModels;

namespace ScreenTranslator.Views;

public partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;

        PlatformCombo.ItemsSource = Enum.GetValues<AiPlatform>();
        WindowModeCombo.ItemsSource = new[]
        {
            new EnumDisplayItem<WindowMode>(WindowMode.Independent, "独立窗口"),
            new EnumDisplayItem<WindowMode>(WindowMode.Sidebar, "侧边栏")
        };
        SidebarPositionCombo.ItemsSource = new[]
        {
            new EnumDisplayItem<SidebarPosition>(SidebarPosition.Left, "左"),
            new EnumDisplayItem<SidebarPosition>(SidebarPosition.Right, "右"),
            new EnumDisplayItem<SidebarPosition>(SidebarPosition.Top, "上"),
            new EnumDisplayItem<SidebarPosition>(SidebarPosition.Bottom, "下")
        };
        ThemeCombo.ItemsSource = new[]
        {
            new EnumDisplayItem<AppTheme>(AppTheme.Dark, "深色"),
            new EnumDisplayItem<AppTheme>(AppTheme.Light, "浅色")
        };

        Loaded += (_, _) =>
        {
            ApiKeyBox.Password = viewModel.AiApiKey;
        };
    }

    private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.AiApiKey = ApiKeyBox.Password;
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveCommand.Execute(null);
        DialogResult = true;
        Close();
    }

    private void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        var key = (e.Key == Key.System) ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        var modifiers = Keyboard.Modifiers;
        var parts = new System.Collections.Generic.List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        parts.Add(key.ToString());

        ViewModel.Hotkey = string.Join("+", parts);
    }

    private void HotkeyBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.XButton1 || e.ChangedButton == MouseButton.XButton2)
        {
            e.Handled = true;
            var buttonName = e.ChangedButton == MouseButton.XButton1 ? "XButton1" : "XButton2";

            var modifiers = Keyboard.Modifiers;
            var parts = new System.Collections.Generic.List<string>();
            if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            parts.Add(buttonName);

            ViewModel.Hotkey = string.Join("+", parts);
        }
    }

    private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
    {
        ((System.Windows.Controls.TextBox)sender).Text = "请按下快捷键...";
    }

    private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var box = (System.Windows.Controls.TextBox)sender;
        if (box.Text == "请按下快捷键...")
            box.Text = ViewModel.Hotkey;
    }

    private void PresetColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string color)
            ViewModel.AccentColor = color;
    }

    private void BrowseBackgroundImage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.webp|所有文件|*.*",
            Title = "选择背景图片"
        };
        if (dlg.ShowDialog() == true)
            ViewModel.BackgroundImagePath = dlg.FileName;
    }
}
