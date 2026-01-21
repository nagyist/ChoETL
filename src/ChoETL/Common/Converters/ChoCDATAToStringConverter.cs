using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
#if _ALL_NET_
using System.Windows.Data;
#endif

namespace ChoETL
{
    [ChoTypeConverter(typeof(ChoCDATA))]
#if _ALL_NET_
    public class ChoCDATAToStringConverter : IValueConverter
#else
    public class ChoCDATAToStringConverter : IChoValueConverter
#endif
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || value is string)
                return new ChoCDATA(value as string);

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ChoCDATA && targetType == typeof(string))
            {
                return ((ChoCDATA)value).Value;
            }
            else if (value == DBNull.Value)
                return null;

            return value;
        }
    }
}
