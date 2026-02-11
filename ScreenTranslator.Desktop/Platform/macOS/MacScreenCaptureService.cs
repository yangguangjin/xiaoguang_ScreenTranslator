using System.Diagnostics;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Desktop.Platform.macOS;

public class MacScreenCaptureService : IScreenCaptureService
{
    public byte[] CaptureRegion(ScreenRegion region)
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"screentranslator_{Guid.NewGuid()}.png");
        try
        {
            var args = $"-R {region.X},{region.Y},{region.Width},{region.Height} {tmpFile}";
            var psi = new ProcessStartInfo("screencapture", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(5000);

            if (File.Exists(tmpFile))
                return File.ReadAllBytes(tmpFile);

            return Array.Empty<byte>();
        }
        finally
        {
            if (File.Exists(tmpFile))
                File.Delete(tmpFile);
        }
    }

    public byte[] CaptureFullScreen(RectInfo bounds)
    {
        var region = new ScreenRegion
        {
            X = bounds.X, Y = bounds.Y,
            Width = bounds.Width, Height = bounds.Height
        };
        return CaptureRegion(region);
    }
}
