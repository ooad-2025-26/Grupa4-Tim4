using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ETFPay.Models;
using ETFPay.Services;

namespace ETFPay.Controllers
{
    [Authorize(Roles = "Admin,Uposlenik,Client")]
    [Route("Currencies")]
    public class KursController : Controller
    {
        private readonly KursService _kursService;

        public KursController(KursService kursService)
        {
            _kursService = kursService;
        }

        [HttpGet("")]
        public async Task<IActionResult> KursView(string? iz, string? u, double? iznos, string? pretraga)
        {
            var kursnaLista = await _kursService.DobaviKursnuListu("BAM");

            if (kursnaLista == null) kursnaLista = new List<Models.Kurs>();

            var aktuelneValute = new List<string> {
                "EUR", "AUD", "CAD", "CZK", "DKK", "HUF", "JPY", "NOK",
                "SEK", "CHF", "TRY", "GBP", "USD", "RUB", "CNY", "RSD"
            };

            if (kursnaLista != null && !string.IsNullOrEmpty(pretraga))
            {
                string kraticaZaPretragu = pretraga.Trim().ToUpper();

                ViewBag.RezultatPretrage = kursnaLista
                    .FirstOrDefault(k => k.CiljanaValuta.ToUpper() == kraticaZaPretragu);

                ViewBag.Pretraga = pretraga;
            }
            else
            {
                ViewBag.RezultatPretrage = null;
            }

            ViewBag.NaziviValuta = kursnaLista.ToDictionary(
                k => k.CiljanaValuta.ToUpper(),
                k => DajPuniNazivValute(k.CiljanaValuta)
            );

            bool jeValidno = true;

            if (HttpContext.Request.Query.ContainsKey("iz") || HttpContext.Request.Query.ContainsKey("u") || HttpContext.Request.Query.ContainsKey("iznos"))
            {
                if (string.IsNullOrWhiteSpace(iz) || string.IsNullOrWhiteSpace(u))
                {
                    ModelState.AddModelError("KonverzijaGreska", "Both currency fields are required.");
                    jeValidno = false;
                }
                else
                {
                    string izUpper = iz.Trim().ToUpper();
                    string uUpper = u.Trim().ToUpper();

                    var sviDostupniKodovi = kursnaLista.Select(k => k.CiljanaValuta.ToUpper()).ToList();
                    sviDostupniKodovi.Add("BAM");

                    if (!sviDostupniKodovi.Contains(izUpper) || !sviDostupniKodovi.Contains(uUpper))
                    {
                        ModelState.AddModelError("KonverzijaGreska", "One or both entered currencies are invalid or not supported.");
                        jeValidno = false;
                    }
                }

                if (!iznos.HasValue || iznos <= 0)
                {
                    ModelState.AddModelError("KonverzijaGreska", "The amount must be a valid number greater than 0.");
                    jeValidno = false;
                }
            }

            if (jeValidno && iznos.HasValue && iznos > 0 && !string.IsNullOrEmpty(iz) && !string.IsNullOrEmpty(u))
            {
                string izUpper = iz.Trim().ToUpper();
                string uUpper = u.Trim().ToUpper();

                if (izUpper == uUpper)
                {
                    ViewBag.Rezultat = iznos.Value;
                }
                else
                {
                    double rezultat = await _kursService.KonvertujIznos(iz, u, (double)iznos);
                    ViewBag.Rezultat = Math.Floor(rezultat * 10000) / 10000;
                }

                ViewBag.Iznos = iznos;
                ViewBag.Iz = iz;
                ViewBag.U = u;
            }
            else
            {
                ViewBag.Rezultat = "";
                ViewBag.Iznos = iznos ?? 1;
                ViewBag.Iz = string.IsNullOrEmpty(iz) ? "BAM" : iz;
                ViewBag.U = string.IsNullOrEmpty(u) ? "EUR" : u;
            }

            var filtriranaListaZaView = kursnaLista
                .Where(k => aktuelneValute.Contains(k.CiljanaValuta.ToUpper()))
                .ToList();

            return View("KursView", filtriranaListaZaView);
        }

        private static string DajPuniNazivValute(string kodValute)
        {
            return kodValute.ToUpper() switch
            {
                "EUR" => "Euro",
                "AUD" => "Australian dollar",
                "CAD" => "Canadian dollar",
                "CZK" => "Czech koruna",
                "DKK" => "Danish krone",
                "HUF" => "Hungarian forint",
                "JPY" => "Japanese yen",
                "NOK" => "Norwegian krone",
                "SEK" => "Swedish krona",
                "CHF" => "Swiss franc",
                "TRY" => "Turkish lira",
                "GBP" => "British pound",
                "USD" => "United States dollar",
                "RUB" => "Russian ruble",
                "CNY" => "Chinese yuan",
                "RSD" => "Serbian dinar",
                _ => ""
            };
        }
    }
}