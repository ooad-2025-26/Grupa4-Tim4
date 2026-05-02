namespace ETFPay.Models
{
    public class Kurs
    {
        public Kurs() { }
        public int Id { get; set; }
        public String Valuta { get; set; }
        public Double IznosZaJedanUSD { get; set; }
        public DateTime VrijemeAzuriranja { get; set; }
    }
}
