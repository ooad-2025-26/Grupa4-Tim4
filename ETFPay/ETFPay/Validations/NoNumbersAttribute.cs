using System.ComponentModel.DataAnnotations;

namespace ETFPay.Validations
{
    public class NoNumbersAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var fieldName = validationContext.DisplayName ?? validationContext.MemberName;
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult($"{fieldName} must not be empty.");
            }

            var text = value.ToString();

            if (text.Any(char.IsDigit))
            {
                return new ValidationResult($"{fieldName} cannot contain numbers.");
            }

            return ValidationResult.Success;
        }
    }
}
