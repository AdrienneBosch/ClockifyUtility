using System;
using System.Globalization;
using System.Windows.Data;

namespace ClockifyUtility.Controls
{
    public class SubtractValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is double total && values[1] is double subtract)
            {
                return Math.Max(0, total - subtract);
            }
            return 0d;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
