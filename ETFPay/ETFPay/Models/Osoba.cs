using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;

namespace ETFPay.Models
{
    public class Osoba : IdentityUser
    {
        public Osoba() { }

        public String Ime { get; set; }
        public String Prezime { get; set; }
        public String JMBG { get; set; }
        public DateOnly DatumRodjenja { get; set; }
        public String Username { get; set; }
        public String Email { get; set; }
        public String Password { get; set; }
        public String BrojTelefona { get; set; }
        [Key]
        public String Id { get; set; }
        public Uloga Uloga { get; set; }
    }
}
