using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.ViewModels;

namespace ScreenTranslator.Desktop.Views;

public partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }
    public bool Saved { get; private set; }

    public SettingsWindow() : this(null!) { }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;

        PlatformCombo.ItemsSource = Enum.GetValues<AiPlatform>();
        ThemeCombo.ItemsSource = Enum.GetValues<AppTheme>();
    }

    private void HotkeyBox_KeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        var modifiers = e.KeyModifiers;
        var parts = new List<string>();
        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        parts.Add(key.ToString());

        ViewModel.Hotkey = string.Join("+", parts);
    }

    private void SaveBtn_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SaveCommand.Execute(null);
        Saved = true;
        Close();
    }
}
