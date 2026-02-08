using WinForms = System.Windows.Forms;

namespace ScreenTranslator.Services;

public class MonitorInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public System.Drawing.Rectangle Bounds { get; set; }
    public System.Drawing.Rectangle WorkingArea { get; set; }
    public bool IsPrimary { get; set; }
}

public class MonitorService
{
    public List<MonitorInfo> GetAllMonitors()
    {
        return WinForms.Screen.AllScreens.Select(s => new MonitorInfo
        {
            DeviceName = s.DeviceName,
            Bounds = s.Bounds,
            WorkingArea = s.WorkingArea,
            IsPrimary = s.Primary
        }).ToList();
    }

    public System.Drawing.Rectangle GetVirtualScreenBounds()
    {
        return WinForms.SystemInformation.VirtualScreen;
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
}
