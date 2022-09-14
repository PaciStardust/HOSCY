using System.Globalization;
using System.Windows.Controls;

namespace Hoscy.Ui
{
    public class IntegerValidationRule : ValidationRule
    {
        public int Min { get; set; } = 0;
        public int Max { get; set; } = 100;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string text = (string)value;

            if (string.IsNullOrWhiteSpace(text))
                return new(false, "Value cannot be empty");

            if (!int.TryParse(text, out var number))
                return new(false, "Value not a valid number");

            if (number > Max || number < Min)
                return new(false, $"Value must be at least {Min} and not larger than {Max}");

            return new(true, null);
        }
    }

    public class FloatValidationRule : ValidationRule
    {
        public float Min { get; set; } = 0;
        public float Max { get; set; } = 100;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string text = (string)value;

            if (string.IsNullOrWhiteSpace(text))
                return new(false, "Value cannot be empty");

            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                return new(false, "Value not a valid number");

            if (number > Max || number < Min)
                return new(false, $"Value must be at least {Min} and not larger than {Max}");

            return new(true, null);
        }
    }
}
