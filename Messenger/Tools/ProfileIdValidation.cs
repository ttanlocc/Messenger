using Mikodev.Network;
using System.Globalization;
using System.Windows.Controls;

namespace Messenger.Tools
{
    internal class ProfileIdValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var str = value as string;
            if (string.IsNullOrEmpty(str))
                return new ValidationResult(false, "Input is empty");
            if (int.TryParse(str, out var id))
                if (Links.Id < id && id < Links.DefaultId)
                    return new ValidationResult(true, string.Empty);
                else
                    return new ValidationResult(false, $"Number should be greater than {Links.Id} and less than {Links.DefaultId}");
            return new ValidationResult(false, "Invalid input");
        }
    }
}
