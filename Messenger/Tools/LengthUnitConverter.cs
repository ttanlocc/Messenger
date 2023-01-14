using Messenger.Extensions;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Messenger.Tools
{
    internal class LengthUnitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Extension.ToUnitEx(System.Convert.ToInt64(value));

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new InvalidOperationException();
    }
}
