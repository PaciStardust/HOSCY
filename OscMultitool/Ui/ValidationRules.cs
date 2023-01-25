using System.Globalization;
using System.Windows.Controls;

namespace Hoscy.Ui
{
    internal class IntegerValidationRule : ValidationRule
    {
        internal int Min { get; set; } = 0;
        internal int Max { get; set; } = 100;

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

    internal class FloatValidationRule : ValidationRule
    {
        internal float Min { get; set; } = 0;
        internal float Max { get; set; } = 100;

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

    internal class StringValidationRule : ValidationRule
    {
        internal float Min { get; set; } = 0;
        internal float Max { get; set; } = 3;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string text = (string)value;
            var strLen = text.Length;

            if (text == null)
                return new(false, "Value cannot be null");

            if (strLen > Max || strLen < Min)
                return new(false, $"Text must be at least {Min} characters and not longer than {Max}");

            return new(true, null);
        }
    }
}
