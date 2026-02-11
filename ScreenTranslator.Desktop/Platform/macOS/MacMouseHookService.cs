using SharpHook;
using SharpHook.Data;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Desktop.Platform.macOS;

public class MacMouseHookService : IMouseHookService
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
