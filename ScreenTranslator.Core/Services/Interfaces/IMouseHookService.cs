namespace ScreenTranslator.Core.Services.Interfaces;

public interface IMouseHookService : IDisposable
{
    event EventHandler<int>? MouseSideButtonPressed;
    void Install();
    void Uninstall();
}
