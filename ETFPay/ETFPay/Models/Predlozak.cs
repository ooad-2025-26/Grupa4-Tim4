using System.ComponentModel.DataAnnotations;
using ETFPay.Validations;

namespace ETFPay.Models
{
    public class Predlozak
    {
        public Predlozak() { }

        [Key]
        public String Id { get; set; }

        [NoNumbers]
        [Display(Name = "Name")]
        public String Naziv { get; set; }

        [PrimaocExists]
        [Display(Name = "Recipient Account Number")]
        public String Primaoc { get; set; }

        [Display(Name = "Payment purpose")]
        public String SvrhaUplate { get; set; }

        [Display(Name = "Address")]
        public String Adresa { get; set; }

        [NoNumbers]
        [Display(Name = "City")]
        public String Grad { get; set; }

        [Display(Name = "Account number")]
        public String BrojRacuna { get; set; }

        [Display(Name = "Amount")]
        public Double Iznos { get; set; }

        [Display(Name = "Subscription")]
        public Boolean Pretplata { get; set; }

        [Display(Name = "Period")]
        public Period Period { get; set; }

        [Display(Name = "Last Payment")]
        public DateOnly PosljednjePlacanje { get; set; }
    }
}
