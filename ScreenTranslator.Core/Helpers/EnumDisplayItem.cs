namespace ScreenTranslator.Core.Helpers;

public class EnumDisplayItem<T>(T value, string display) where T : struct, Enum
{
    public T Value { get; } = value;
    public string Display { get; } = display;
    public override string ToString() => Display;
}
