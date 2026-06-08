using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
namespace ETFPay.Models
{
    public class Osoba : IdentityUser
    {
        public Osoba() {}

        public String Ime { get; set; }
        public String Prezime { get; set; }
        public String JMBG { get; set; }
        public DateOnly DatumRodjenja { get; set; }
       
        public Double? Plata { get; set; }

        public DateOnly? DatumZaposlenja { get; set; }

        public String? Racun { get; set; } // polje sa ID-em racuna

        public Racun? RacunKorisnika { get; set; }
    }
}
