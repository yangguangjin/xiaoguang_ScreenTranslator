using ScreenTranslator.Core.Models;

namespace ScreenTranslator.Core.Services.Interfaces;

public interface IMonitorService
{
    List<MonitorInfo> GetAllMonitors();
    RectInfo GetVirtualScreenBounds();
    MonitorInfo? GetSecondaryMonitor();
    MonitorInfo? GetMonitorByIndex(int index);
}
