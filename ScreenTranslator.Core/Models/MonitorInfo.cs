namespace ScreenTranslator.Core.Models;

public class RectInfo
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class MonitorInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public RectInfo Bounds { get; set; } = new();
    public RectInfo WorkingArea { get; set; } = new();
    public bool IsPrimary { get; set; }
}
