using ETFPay.Data;
using System.ComponentModel.DataAnnotations;

namespace ETFPay.Validations
{
    public class PrimaocExistsAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var primaoc = value.ToString();

            var dbContext = validationContext.GetService(typeof(ApplicationDbContext)) as ApplicationDbContext;
            if (dbContext == null)
            {
                return new ValidationResult("Unable to validate recipient account.");
            }

            var racunExists = dbContext.Racun.Any(r => r.brojRacuna == primaoc);

            if (!racunExists)
            {
                return new ValidationResult($"Recipient account number '{primaoc}' does not exist.");
            }

            return ValidationResult.Success;
        }
    }
}
