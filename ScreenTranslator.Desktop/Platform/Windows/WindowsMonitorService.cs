using System.Runtime.Versioning;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;
using WinForms = System.Windows.Forms;

namespace ScreenTranslator.Desktop.Platform.Windows;

[SupportedOSPlatform("windows")]
public class WindowsMonitorService : IMonitorService
{
    public List<MonitorInfo> GetAllMonitors()
    {
        return WinForms.Screen.AllScreens.Select(s => new MonitorInfo
        {
            DeviceName = s.DeviceName,
            Bounds = ToRectInfo(s.Bounds),
            WorkingArea = ToRectInfo(s.WorkingArea),
            IsPrimary = s.Primary
        }).ToList();
    }

    public RectInfo GetVirtualScreenBounds()
    {
        var vs = WinForms.SystemInformation.VirtualScreen;
        return ToRectInfo(vs);
    }

    public MonitorInfo? GetSecondaryMonitor()
    {
        var monitors = GetAllMonitors();
        return monitors.FirstOrDefault(m => !m.IsPrimary) ?? monitors.FirstOrDefault();
    }

    public MonitorInfo? GetMonitorByIndex(int index)
    {
        var monitors = GetAllMonitors();
        return index >= 0 && index < monitors.Count ? monitors[index] : null;
    }

    private static RectInfo ToRectInfo(System.Drawing.Rectangle r) => new()
    {
        X = r.X, Y = r.Y, Width = r.Width, Height = r.Height
    };
}
