using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ETFPay.Models
{
    public class Transakcija
    {
        public Transakcija() { }

        [Key]
        public String Id { get; set; } = Guid.NewGuid().ToString();
        [ForeignKey("Racun")]
        public String Primaoc { get; set; }
        [ForeignKey("Racun")]
        public String Posiljaoc { get; set; }
        public Double Iznos { get; set; }
        public DateTime VrijemeTransakcije { get; set; }
        public string? SvrhaUplate { get; set; }
    }
}
