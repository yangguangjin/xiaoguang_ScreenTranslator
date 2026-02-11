using SharpHook;
using SharpHook.Data;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Desktop.Platform.Windows;

public class WindowsMouseHookService : IMouseHookService
{
    private SimpleGlobalHook? _hook;
    private bool _disposed;

    public event EventHandler<int>? MouseSideButtonPressed;

    public void Install()
    {
        if (_hook != null) return;

        _hook = new SimpleGlobalHook();
        _hook.MousePressed += OnMousePressed;
        Task.Run(() => _hook.Run());
    }

    private void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        // SharpHook reports extra buttons as Button4, Button5
        if (e.Data.Button == MouseButton.Button4)
            MouseSideButtonPressed?.Invoke(this, 1);
        else if (e.Data.Button == MouseButton.Button5)
            MouseSideButtonPressed?.Invoke(this, 2);
    }

    public void Uninstall()
    {
        _hook?.Dispose();
        _hook = null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Uninstall();
            _disposed = true;
        }
    }
}
