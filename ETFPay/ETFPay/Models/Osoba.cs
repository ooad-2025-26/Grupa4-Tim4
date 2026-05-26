using Microsoft.AspNetCore.Identity;
namespace ETFPay.Models
{
    public class Osoba : IdentityUser
    {
        public Osoba() { }

        public String Ime { get; set; }
        public String Prezime { get; set; }
        public String JMBG { get; set; }
        public DateOnly DatumRodjenja { get; set; }
       
      
    }
}
