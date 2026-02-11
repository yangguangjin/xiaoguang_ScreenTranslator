namespace ScreenTranslator.Core.Services.Interfaces;

public interface IPlatformService
{
    string GetSettingsDirectory();
    bool EnsureSingleInstance();
    void ReleaseSingleInstance();
    void SetAutoStart(bool enable);
    void SetClickThrough(nint windowHandle, bool enable);
}
