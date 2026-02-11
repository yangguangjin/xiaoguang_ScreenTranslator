using ScreenTranslator.Core.Models;

namespace ScreenTranslator.Core.Services.Interfaces;

public interface IScreenCaptureService
{
    byte[] CaptureRegion(ScreenRegion region);
    byte[] CaptureFullScreen(RectInfo bounds);
}
