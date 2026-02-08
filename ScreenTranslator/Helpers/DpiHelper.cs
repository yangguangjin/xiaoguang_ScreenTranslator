using System.Windows;
using System.Windows.Media;

namespace ScreenTranslator.Helpers;

public static class DpiHelper
{
    public static (double dpiX, double dpiY) GetDpi(Visual visual)
    {
        var source = PresentationSource.FromVisual(visual);
        if (source?.CompositionTarget != null)
        {
            return (source.CompositionTarget.TransformToDevice.M11,
                    source.CompositionTarget.TransformToDevice.M22);
        }
        return (1.0, 1.0);
    }

    public static System.Windows.Point WpfToPhysical(System.Windows.Point wpfPoint, double dpiX, double dpiY)
    {
        return new System.Windows.Point(wpfPoint.X * dpiX, wpfPoint.Y * dpiY);
    }

    public static System.Windows.Point PhysicalToWpf(System.Windows.Point physicalPoint, double dpiX, double dpiY)
    {
        return new System.Windows.Point(physicalPoint.X / dpiX, physicalPoint.Y / dpiY);
    }

    public static Rect PhysicalToWpf(System.Drawing.Rectangle rect, double dpiX, double dpiY)
    {
        return new Rect(
            rect.X / dpiX,
            rect.Y / dpiY,
            rect.Width / dpiX,
            rect.Height / dpiY);
    }
}
