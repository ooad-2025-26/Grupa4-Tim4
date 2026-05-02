using System.ComponentModel.DataAnnotations;

namespace ETFPay.Models
{
    public class Predlozak
    {
        public Predlozak() { }

        [Key]
        public String Id { get; set; }
        public String Naziv { get; set; }
        public String Primaoc { get; set; }
        public String SvrhaUplate { get; set; }
        public String Adresa { get; set; }
        public String Grad { get; set; }
        public String BrojRacuna { get; set; }
        public Double Iznos { get; set; }
        public Boolean Pretplata { get; set; }
        public Period Period { get; set; }
    }
}
