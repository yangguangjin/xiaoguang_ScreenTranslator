using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;

namespace ScreenTranslator.Services;

public class HotkeyService : IDisposable
{
    private const string HotkeyName = "ScreenTranslator_Translate";
    private bool _registered;
    private bool _isMouseHotkey;
    private int _mouseButton; // 1=XButton1, 2=XButton2
    private ModifierKeys _mouseModifiers;
    private MouseHookService? _mouseHookService;

    public event EventHandler? HotkeyPressed;

    public void SetMouseHookService(MouseHookService service)
    {
        _mouseHookService = service;
        _mouseHookService.MouseSideButtonPressed += OnMouseSideButtonPressed;
    }

    private void OnMouseSideButtonPressed(object? sender, int xButton)
    {
        if (!_isMouseHotkey || xButton != _mouseButton) return;

        var currentModifiers = ModifierKeys.None;
        if ((System.Windows.Input.Keyboard.Modifiers & ModifierKeys.Control) != 0)
            currentModifiers |= ModifierKeys.Control;
        if ((System.Windows.Input.Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            currentModifiers |= ModifierKeys.Alt;
        if ((System.Windows.Input.Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            currentModifiers |= ModifierKeys.Shift;

        if (currentModifiers == _mouseModifiers)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                HotkeyPressed?.Invoke(this, EventArgs.Empty));
        }
    }

    public bool Register(string hotkeyString)
    {
        Unregister();

        if (TryParseMouseHotkey(hotkeyString, out var mouseButton, out var mouseModifiers))
        {
            _isMouseHotkey = true;
            _mouseButton = mouseButton;
            _mouseModifiers = mouseModifiers;
            _registered = true;
            _mouseHookService?.Install();
            return true;
        }

        if (!TryParseHotkey(hotkeyString, out var key, out var modifiers))
            return false;

        _isMouseHotkey = false;

        try
        {
            HotkeyManager.Current.AddOrReplace(HotkeyName, key, modifiers, OnHotkeyPressed);
            _registered = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Unregister()
    {
        if (!_registered) return;
        if (!_isMouseHotkey)
        {
            try
            {
                HotkeyManager.Current.Remove(HotkeyName);
            }
            catch { }
        }
        _registered = false;
        _isMouseHotkey = false;
    }

    private void OnHotkeyPressed(object? sender, HotkeyEventArgs e)
    {
        HotkeyPressed?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    public static bool TryParseMouseHotkey(string hotkeyString, out int mouseButton, out ModifierKeys modifiers)
    {
        mouseButton = 0;
        modifiers = ModifierKeys.None;

        if (string.IsNullOrWhiteSpace(hotkeyString))
            return false;

        var parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries);
        bool foundMouse = false;

        foreach (var part in parts)
        {
            switch (part.ToLower())
            {
                case "ctrl" or "control":
                    modifiers |= ModifierKeys.Control;
                    break;
                case "alt":
                    modifiers |= ModifierKeys.Alt;
                    break;
                case "shift":
                    modifiers |= ModifierKeys.Shift;
                    break;
                case "xbutton1":
                    mouseButton = 1;
                    foundMouse = true;
                    break;
                case "xbutton2":
                    mouseButton = 2;
                    foundMouse = true;
                    break;
            }
        }

        return foundMouse;
    }

    public static bool TryParseHotkey(string hotkeyString, out Key key, out ModifierKeys modifiers)
    {
        key = Key.None;
        modifiers = ModifierKeys.None;

        if (string.IsNullOrWhiteSpace(hotkeyString))
            return false;

        var parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToLower())
            {
                case "ctrl" or "control":
                    modifiers |= ModifierKeys.Control;
                    break;
                case "alt":
                    modifiers |= ModifierKeys.Alt;
                    break;
                case "shift":
                    modifiers |= ModifierKeys.Shift;
                    break;
                case "win" or "windows":
                    modifiers |= ModifierKeys.Windows;
                    break;
                default:
                    if (Enum.TryParse<Key>(part, true, out var parsedKey))
                        key = parsedKey;
                    else
                        return false;
                    break;
            }
        }

        return key != Key.None;
    }

    public void Dispose()
    {
        Unregister();
        if (_mouseHookService != null)
        {
            _mouseHookService.MouseSideButtonPressed -= OnMouseSideButtonPressed;
        }
    }
}
