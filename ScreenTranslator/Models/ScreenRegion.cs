namespace ScreenTranslator.Models;

public class ScreenRegion
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public System.Drawing.Rectangle ToRectangle() => new(X, Y, Width, Height);
}
