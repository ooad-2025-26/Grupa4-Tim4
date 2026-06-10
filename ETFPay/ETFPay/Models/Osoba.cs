using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ETFPay.Models
{
    public class Osoba : IdentityUser
    {
        public Osoba() { }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 20 characters long.")]
        [RegularExpression(@"^[a-zA-ZčćšđžČĆŠĐŽа-яА-Я]+$", ErrorMessage = "First name can only contain letters.")]
        [Display(Name = "First Name")]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 30 characters long.")]
        [RegularExpression(@"^[a-zA-ZčćšđžČĆŠĐŽа-яА-Я\s-]+$", ErrorMessage = "Last name can only contain letters, spaces, or hyphens.")]
        [Display(Name = "Last Name")]
        public string Prezime { get; set; }

        [Required(ErrorMessage = "JMBG is required.")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "JMBG must be exactly 13 digits long.")]
        [RegularExpression(@"^(0[1-9]|[12][0-9]|3[01])(0[1-9]|1[012])[0-9]{3}[0-9]{6}$", ErrorMessage = "Invalid JMBG format (first digits must represent a valid date, 13 digits total).")]
        public string JMBG { get; set; }

        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date)]
        [StarostValidacija(16, ErrorMessage = "You must be at least 16 years old.")]
        [Display(Name = "Date of Birth")]
        public DateOnly DatumRodjenja { get; set; }

        [Range(0.0, double.MaxValue, ErrorMessage = "Salary must be a positive number.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Salary")]
        public double? Plata { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Employment")]
        public DateOnly? DatumZaposlenja { get; set; }

        public string? Racun { get; set; } 

        public Racun? RacunKorisnika { get; set; }
    }

    //Validacija godina
    public class StarostValidacijaAttribute : ValidationAttribute
    {
        private readonly int _minGodina;
        public StarostValidacijaAttribute(int minGodina)
        {
            _minGodina = minGodina;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateOnly datumRodjenja)
            {
                var danas = DateOnly.FromDateTime(DateTime.Today);
                var starost = danas.Year - datumRodjenja.Year;

               
                if (datumRodjenja > danas.AddYears(-starost))
                {
                    starost--;
                }

                if (starost < _minGodina)
                {
                    return new ValidationResult(ErrorMessage ?? $"You must be at least {_minGodina} years old.");
                }
            }
            return ValidationResult.Success;
        }
    }
}