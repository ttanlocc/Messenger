using System.Globalization;
using System.Net;
using System.Windows.Controls;

namespace Messenger.Tools
{
    internal class SocketPortValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var str = value as string;
            if (string.IsNullOrEmpty(str))
                return new ValidationResult(false, "Input is empty");
            if (int.TryParse(str, out var val))
                if (val >= IPEndPoint.MinPort && val <= IPEndPoint.MaxPort)
                    return new ValidationResult(true, string.Empty);
                else
                    return new ValidationResult(false, $"The port number should be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}");
            return new ValidationResult(false, "Invalid input");
        }
    }
}
