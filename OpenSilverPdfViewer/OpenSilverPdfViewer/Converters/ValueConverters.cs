
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

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
