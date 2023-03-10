using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Messenger.Tools
{
    internal class LogicToPixelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visual vis && targetType == typeof(Thickness) && parameter is Thickness mar)
            {
                var win = PresentationSource.FromVisual(vis);
                var hor = 1.0 / win.CompositionTarget.TransformToDevice.M11;
                var ver = 1.0 / win.CompositionTarget.TransformToDevice.M22;
                var thi = new Thickness(hor * mar.Left, ver * mar.Top, hor * mar.Right, ver * mar.Bottom);
                return thi;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new InvalidOperationException();
    }
}
