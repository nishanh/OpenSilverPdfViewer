using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace OpenSilverPdfViewer.Converters
{
    public class ValueConverterBase<T> : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
    public class BoolToVisibilityConverter : ValueConverterBase<bool>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return base.Convert(value, targetType, parameter, culture);
        }
    }
    public class BoolToEnabledConverterInvert : ValueConverterBase<bool>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? false : true;
            }
            return base.Convert(value, targetType, parameter, culture);
        }
    }
}
