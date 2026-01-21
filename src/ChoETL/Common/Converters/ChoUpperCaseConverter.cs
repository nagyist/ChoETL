using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if _ALL_NET_
using System.Windows.Data;
#endif

namespace ChoETL
{
#if _ALL_NET_
    public class ChoUpperCaseConverter : IValueConverter
#else
    public class ChoUpperCaseConverter : IChoValueConverter
#endif
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                return ((string)value).ToUpper();
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string && targetType == typeof(string))
            {
                return ((string)value).ToUpper();
            }
            return value;
        }
    }
}
