using System.Drawing;
using System.Drawing.Imaging;
using ScreenTranslator.Models;

namespace ScreenTranslator.Services;

public class ScreenCaptureService
{
    public Bitmap CaptureRegion(ScreenRegion region)
    {
        var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(region.X, region.Y, 0, 0, new Size(region.Width, region.Height));
        return bitmap;
    }

    public Bitmap CaptureFullScreen(Rectangle bounds)
    {
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, new Size(bounds.Width, bounds.Height));
        return bitmap;
    }
}
