using System.ComponentModel.DataAnnotations;

namespace ETFPay.Models
{
    public class Racun
    {
        public Racun() { }

        [Key]
        public String Id { get; set; }
        public String brojRacuna { get; set; }
        public String Stanje { get; set; }
        public DateOnly DatumKreiranja { get; set; }
        public String IBAN { get; set; }
        public Boolean Aktivan { get; set; }
    }
}
