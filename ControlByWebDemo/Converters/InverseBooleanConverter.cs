using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ControlByWebDemo.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            try
            {
                bool testValue = (bool)value;
                return !testValue; // or do whatever you need with this boolean
            }
            catch { return true; } // or false
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }

        #endregion
    }
}
