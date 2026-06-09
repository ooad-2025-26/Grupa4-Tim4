

using System.Text.Json.Serialization;

namespace ETFPay.Models
{
    public class Kurs
    {
        [JsonPropertyName("date")]
        public DateOnly Datum { get; set; }

        [JsonPropertyName("base")]
        public String BaznaValuta { get; set; }

        [JsonPropertyName("quote")]
        public String CiljanaValuta { get; set; }
        
        [JsonPropertyName("rate")]
        public Double KursValute { get; set; }
    }
}
