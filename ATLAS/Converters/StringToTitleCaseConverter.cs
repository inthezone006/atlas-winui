using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace ATLAS.Converters
{
    public class StringToTitleCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string str)
            {
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}