using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace SerialCommunication.Converters
{
    public class ColorConverter : IValueConverter
    {
        SolidColorBrush grayBrush = new SolidColorBrush(Colors.Gray);
        SolidColorBrush greenBrush = new SolidColorBrush(Colors.Green);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.ToString() == "未开始")
            {
                return grayBrush;
            }
            else
            {
                return greenBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
