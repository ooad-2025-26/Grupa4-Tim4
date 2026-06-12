namespace ETFPay.Models
{
    public class TransakcijeViewModel
    {
        public List<Transakcija> Transakcije { get; set; } = new();
        public Transakcija? Odabrana { get; set; }
        public string BrojRacunaKorisnika { get; set; } = "";
        public string RacunIdKorisnika { get; set; } = "";
        public string BrojRacunaDrugaStrana { get; set; } = "";
        public Dictionary<string, string> ImeProtivnikaPoTransakciji { get; set; } = new();
    }
}
