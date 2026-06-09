using System.Net.Http.Json;
using ETFPay.Models;

namespace ETFPay.Services
{
    public class KursService
    {
        private readonly HttpClient _httpClient;
        public KursService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Kurs?>> DobaviKursnuListu(string bazna = "BAM")
        {
            try
            {
                string url = $"https://api.frankfurter.dev/v2/rates?base={bazna}";

                var response = await _httpClient.GetFromJsonAsync<List<Kurs>>(url);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska pri dobavljanju kursne liste");
                return null;
            }
        }

        public async Task<double> KonvertujIznos(string iz, string u, double iznos)
        {
            try
            {
                string url = $"https://api.frankfurter.dev/v2/rates?base={iz.ToUpper()}&quotes={u.ToUpper()}";

                var response = await _httpClient.GetFromJsonAsync<List<Kurs>>(url);

                if(response != null && response.Count > 0)
                {
                    return iznos * response[0].KursValute;
                }

                return 0;
            }catch
            {
                return 0;
            }
        }

    }
}
