using System.Globalization;
using System.Windows.Data;

namespace SpotifyRemote.App.Converters
{
    public class BooleanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                // If True (Running) -> "Stop Server"
                // If False (Stopped) -> "Start Server"
                // Actually the XAML handles the text content via triggers, 
                // but the Content binding was used as fallback or base.
                // Let's just return null or the bool string if we rely on Triggers.
                // But the XAML had: Content="{Binding ..., Converter=...}"
                // The Triggers override Content.
                // So this converter might just be used for the initial binding value.
                return b ? "Stop Server" : "Start Server";
            }
            return "Server Action";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
