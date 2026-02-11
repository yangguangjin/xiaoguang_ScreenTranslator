using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Desktop.Platform.Windows;

[SupportedOSPlatform("windows")]
public class WindowsPlatformService : IPlatformService
{
    private Mutex? _mutex;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    public string GetSettingsDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreenTranslator");
    }

    public bool EnsureSingleInstance()
    {
        _mutex = new Mutex(true, "ScreenTranslator_SingleInstance", out bool isNew);
        return isNew;
    }

    public void ReleaseSingleInstance()
    {
        _mutex?.Dispose();
        _mutex = null;
    }

    public void SetAutoStart(bool enable)
    {
        const string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        const string valueName = "ScreenTranslator";
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyName, true);
            if (key == null) return;
            if (enable)
            {
                var exePath = Environment.ProcessPath
                    ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                key.SetValue(valueName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(valueName, false);
            }
        }
        catch { /* ignore registry errors */ }
    }

    public void SetClickThrough(nint windowHandle, bool enable)
    {
        var style = GetWindowLong(windowHandle, GWL_EXSTYLE);
        if (enable)
            SetWindowLong(windowHandle, GWL_EXSTYLE, style | WS_EX_TRANSPARENT);
        else
            SetWindowLong(windowHandle, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
    }
}
