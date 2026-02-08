using System.Globalization;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace ScreenTranslator.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;
        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string s)
            return Enum.Parse(targetType, s);
        return Binding.DoNothing;
    }
}
