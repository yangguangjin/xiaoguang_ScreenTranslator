using SharpHook;
using SharpHook.Data;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Desktop.Platform.macOS;

public class MacHotkeyService : IHotkeyService
{
    private IMouseHookService? _mouseHookService;
    private bool _registered;
    private bool _isMouseHotkey;
    private int _mouseButton;

    private KeyCode _registeredKey;
    private EventMask _registeredModifiers;
    private SimpleGlobalHook? _hook;
    private bool _disposed;

    public event EventHandler? HotkeyPressed;

    public void SetMouseHookService(IMouseHookService service)
    {
        _mouseHookService = service;
        _mouseHookService.MouseSideButtonPressed += OnMouseSideButtonPressed;
    }

    private void OnMouseSideButtonPressed(object? sender, int xButton)
    {
        if (!_isMouseHotkey || xButton != _mouseButton) return;
        HotkeyPressed?.Invoke(this, EventArgs.Empty);
    }

    public bool Register(string hotkeyString)
    {
        Unregister();

        if (TryParseMouseHotkey(hotkeyString, out var mouseButton))
        {
            _isMouseHotkey = true;
            _mouseButton = mouseButton;
            _registered = true;
            _mouseHookService?.Install();
            return true;
        }

        if (!TryParseHotkey(hotkeyString, out var key, out var modifiers))
            return false;

        _isMouseHotkey = false;
        _registeredKey = key;
        _registeredModifiers = modifiers;

        try
        {
            _hook = new SimpleGlobalHook();
            _hook.KeyPressed += OnKeyPressed;
            Task.Run(() => _hook.Run());
            _registered = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode == _registeredKey)
        {
            var currentMods = e.RawEvent.Mask;
            if ((currentMods & _registeredModifiers) == _registeredModifiers)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    HotkeyPressed?.Invoke(this, EventArgs.Empty));
            }
        }
    }

    public void Unregister()
    {
        if (!_registered) return;
        _hook?.Dispose();
        _hook = null;
        _registered = false;
        _isMouseHotkey = false;
    }

    private static bool TryParseMouseHotkey(string hotkeyString, out int mouseButton)
    {
        mouseButton = 0;
        if (string.IsNullOrWhiteSpace(hotkeyString)) return false;

        var parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (part.Equals("XButton1", StringComparison.OrdinalIgnoreCase)) { mouseButton = 1; return true; }
            if (part.Equals("XButton2", StringComparison.OrdinalIgnoreCase)) { mouseButton = 2; return true; }
        }
        return false;
    }

    private static bool TryParseHotkey(string hotkeyString, out KeyCode key, out EventMask modifiers)
    {
        key = KeyCode.VcUndefined;
        modifiers = EventMask.None;

        if (string.IsNullOrWhiteSpace(hotkeyString)) return false;

        var parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToLower())
            {
                case "ctrl" or "control": modifiers |= EventMask.LeftCtrl; break;
                case "alt": modifiers |= EventMask.LeftAlt; break;
                case "shift": modifiers |= EventMask.LeftShift; break;
                case "cmd" or "command": modifiers |= EventMask.LeftMeta; break;
                default:
                    key = MapKeyName(part);
                    break;
            }
        }

        return key != KeyCode.VcUndefined;
    }

    private static KeyCode MapKeyName(string name)
    {
        return name.ToUpper() switch
        {
            "A" => KeyCode.VcA, "B" => KeyCode.VcB, "C" => KeyCode.VcC,
            "D" => KeyCode.VcD, "E" => KeyCode.VcE, "F" => KeyCode.VcF,
            "G" => KeyCode.VcG, "H" => KeyCode.VcH, "I" => KeyCode.VcI,
            "J" => KeyCode.VcJ, "K" => KeyCode.VcK, "L" => KeyCode.VcL,
            "M" => KeyCode.VcM, "N" => KeyCode.VcN, "O" => KeyCode.VcO,
            "P" => KeyCode.VcP, "Q" => KeyCode.VcQ, "R" => KeyCode.VcR,
            "S" => KeyCode.VcS, "T" => KeyCode.VcT, "U" => KeyCode.VcU,
            "V" => KeyCode.VcV, "W" => KeyCode.VcW, "X" => KeyCode.VcX,
            "Y" => KeyCode.VcY, "Z" => KeyCode.VcZ,
            "F1" => KeyCode.VcF1, "F2" => KeyCode.VcF2, "F3" => KeyCode.VcF3,
            "F4" => KeyCode.VcF4, "F5" => KeyCode.VcF5, "F6" => KeyCode.VcF6,
            "F7" => KeyCode.VcF7, "F8" => KeyCode.VcF8, "F9" => KeyCode.VcF9,
            "F10" => KeyCode.VcF10, "F11" => KeyCode.VcF11, "F12" => KeyCode.VcF12,
            "SPACE" => KeyCode.VcSpace,
            "ESCAPE" or "ESC" => KeyCode.VcEscape,
            "ENTER" or "RETURN" => KeyCode.VcEnter,
            _ => KeyCode.VcUndefined
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Unregister();
        if (_mouseHookService != null)
            _mouseHookService.MouseSideButtonPressed -= OnMouseSideButtonPressed;
    }
}
