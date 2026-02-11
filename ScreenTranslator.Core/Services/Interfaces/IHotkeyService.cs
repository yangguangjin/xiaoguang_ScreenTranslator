namespace ScreenTranslator.Core.Services.Interfaces;

public interface IHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;
    bool Register(string hotkeyString);
    void Unregister();
    void SetMouseHookService(IMouseHookService service);
}
