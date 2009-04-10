using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace BlipFace.Helpers
{
    /// 
    /// Convert a boolean to visibility value.
    /// 
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool tmp = (bool)value;
            if (tmp)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility v = (Visibility)value;
            if (v == Visibility.Collapsed || v == Visibility.Hidden)
                return false;
            return true;
        }


    }
}
