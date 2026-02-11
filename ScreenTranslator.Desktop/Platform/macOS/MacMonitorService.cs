using System.Runtime.InteropServices;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Desktop.Platform.macOS;

public class MacMonitorService : IMonitorService
{
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern uint CGGetActiveDisplayList(uint maxDisplays, uint[] activeDisplays, out uint displayCount);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern CGRect CGDisplayBounds(uint display);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern uint CGMainDisplayID();

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect
    {
        public double X, Y, Width, Height;
    }

    public List<MonitorInfo> GetAllMonitors()
    {
        var result = new List<MonitorInfo>();
        var displays = new uint[16];
        CGGetActiveDisplayList(16, displays, out uint count);
        var mainId = CGMainDisplayID();

        for (int i = 0; i < count; i++)
        {
            var bounds = CGDisplayBounds(displays[i]);
            result.Add(new MonitorInfo
            {
                DeviceName = $"Display {displays[i]}",
                Bounds = new RectInfo
                {
                    X = (int)bounds.X, Y = (int)bounds.Y,
                    Width = (int)bounds.Width, Height = (int)bounds.Height
                },
                WorkingArea = new RectInfo
                {
                    X = (int)bounds.X, Y = (int)bounds.Y,
                    Width = (int)bounds.Width, Height = (int)bounds.Height
                },
                IsPrimary = displays[i] == mainId
            });
        }

        return result;
    }

    public RectInfo GetVirtualScreenBounds()
    {
        var monitors = GetAllMonitors();
        if (monitors.Count == 0)
            return new RectInfo { Width = 1920, Height = 1080 };

        int minX = monitors.Min(m => m.Bounds.X);
        int minY = monitors.Min(m => m.Bounds.Y);
        int maxX = monitors.Max(m => m.Bounds.X + m.Bounds.Width);
        int maxY = monitors.Max(m => m.Bounds.Y + m.Bounds.Height);

        return new RectInfo
        {
            X = minX, Y = minY,
            Width = maxX - minX, Height = maxY - minY
        };
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
