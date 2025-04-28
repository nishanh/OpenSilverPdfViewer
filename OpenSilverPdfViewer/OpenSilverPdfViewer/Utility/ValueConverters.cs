
// Copyright (c) 2025 Nishan Hossepian. All rights reserved.              
// Free to use, modify, and distribute under the terms of the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace OpenSilverPdfViewer.Utility
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
    public sealed class BoolToVisibilityConverter : ValueConverterBase<bool>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                var condition = parameter == null || bool.Parse(parameter as string);
                return boolValue == condition ? Visibility.Visible : Visibility.Collapsed;
            }
            return base.Convert(value, targetType, parameter, culture);
        }
    }
    public sealed class BoolToEnabledConverter : ValueConverterBase<bool>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                var condition = parameter == null || bool.Parse(parameter as string);
                return boolValue == condition;
            }
            return base.Convert(value, targetType, parameter, culture);
        }
    }
    public sealed class RenderModeToBoolConverter : ValueConverterBase<RenderModeType>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RenderModeType renderModeType)
            {
                return renderModeType == (RenderModeType)parameter;
            }
            return false;
        }
    }
    public sealed class RulerUnitsToBoolConverter : ValueConverterBase<UnitMeasure>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UnitMeasure rulerUnits)
            {
                return rulerUnits == (UnitMeasure)parameter;
            }
            return false;
        }
    }
    public sealed class IntToEnabledConverter : ValueConverterBase<int>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int propValue)
            {
                return propValue > 0;
            }
            return false;
        }
    }
    public sealed class ViewModeToCheckedConverter : ValueConverterBase<ViewModeType>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ViewModeType renderModeType)
            {
                return renderModeType == (ViewModeType)parameter;
            }
            return false;
        }
    }
}
