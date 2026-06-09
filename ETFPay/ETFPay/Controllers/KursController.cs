using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ETFPay.Data;
using ETFPay.Models;
using ETFPay.Services;

namespace ETFPay.Controllers
{
    public class KursController : Controller
    {
        private readonly KursService _kursService;

        private readonly string[] AktuelneValute = {
            "EUR", "AUD", "CAD", "CZK", "DKK", "HUF", "JPY", "NOK",
            "SEK", "CHF", "TRY", "GBP", "USD", "RUB", "CNY", "RSD"
        };

        public KursController(KursService kursService)
        {
            _kursService = kursService;
        }

        public async Task<IActionResult> Index(string? iz, string? u, double? iznos, string? pretraga)
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

            if (iznos.HasValue && iznos > 0)
            {
                double rezultat = await _kursService.KonvertujIznos(iz, u, (double)iznos);

                ViewBag.Rezultat = Math.Floor(rezultat * 10000) / 10000;
                ViewBag.Iznos = iznos;
                ViewBag.Iz = iz;
                ViewBag.U = u;
            }
            else
            {
                ViewBag.Rezultat = "";
                ViewBag.Iznos = 1;
                ViewBag.Iz = "BAM";
                ViewBag.U = "EUR";
            }

            var filtriranaListaZaView = kursnaLista
                .Where(k => aktuelneValute.Contains(k.CiljanaValuta.ToUpper()))
                .ToList();

            return View(filtriranaListaZaView);
        }

        private static string DajPuniNazivValute(string kodValute)
        {
            return kodValute.ToUpper() switch
            {
                "EUR" => "Euro",
                "AUD" => "Australijski dolar",
                "CAD" => "Kanadski dolar",
                "CZK" => "Češka kruna",
                "DKK" => "Danska kruna",
                "HUF" => "Mađarska forinta",
                "JPY" => "Japanski jen",
                "NOK" => "Norveška kruna",
                "SEK" => "Švedska kruna",
                "CHF" => "Švicarski franak",
                "TRY" => "Turska lira",
                "GBP" => "Britanska funta",
                "USD" => "Američki dolar",
                "RUB" => "Ruska rublja",
                "CNY" => "Kineski juan",
                "RSD" => "Srpski dinar",
                _ => ""
            };
        }
    }
}
