namespace ScreenTranslator.Core.Models;

public class ScreenRegion
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public RectInfo ToRectInfo() => new() { X = X, Y = Y, Width = Width, Height = Height };
}
