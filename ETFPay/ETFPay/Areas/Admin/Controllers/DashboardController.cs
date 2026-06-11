using System.ComponentModel.DataAnnotations;
using ETFPay.Data;
using ETFPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ETFPay.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private static readonly string[] DozvoljeneUloge =
            { "Uposlenik", "Zastitar", "Direktor", "Domar", "Blagajnik", "Admin" };

        private readonly UserManager<Osoba> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(
            UserManager<Osoba> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? id, string? mode)
        {
            return View(await BuildViewModelAsync(id, mode, new NoviUposlenikForm(), null));
        }

        [HttpGet]
        public async Task<IActionResult> UserSearch(string? q, string? filter, string? id)
        {
            return View(await BuildUserSearchViewModelAsync(q, filter, id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KreirajUposlenika([Bind(Prefix = "Novi")] NoviUposlenikForm novi)
        {
            var dijelovi = (novi.ImePrezime ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (dijelovi.Length < 2)
                ModelState.AddModelError("Novi.ImePrezime", "Unesite ime i prezime.");

            var uloga = string.IsNullOrWhiteSpace(novi.Uloga) ? "Uposlenik" : novi.Uloga.Trim();
            if (!DozvoljeneUloge.Contains(uloga))
                ModelState.AddModelError("Novi.Uloga", "Odabrana uloga nije validna.");

            if (!ModelState.IsValid)
                return View("Index", await BuildViewModelAsync(null, "novi", novi, null));

            var username = novi.Username.Trim();
            if (await _userManager.FindByNameAsync(username) != null)
            {
                ModelState.AddModelError("Novi.Username", "Username je već zauzet.");
                return View("Index", await BuildViewModelAsync(null, "novi", novi, null));
            }

            var user = new Osoba
            {
                Ime = dijelovi[0],
                Prezime = dijelovi[1],
                UserName = username,
                Email = $"{username}@etfpay.local",
                EmailConfirmed = true,
                PhoneNumber = novi.BrojTelefona,
                Plata = novi.Plata,
                DatumZaposlenja = DateOnly.FromDateTime(DateTime.Today),
                JMBG = GenerisiJmbg(),
                DatumRodjenja = DateOnly.FromDateTime(DateTime.Today.AddYears(-25))
            };

            var result = await _userManager.CreateAsync(user, novi.Sifra);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View("Index", await BuildViewModelAsync(null, "novi", novi, null));
            }

            if (!await _roleManager.RoleExistsAsync(uloga))
                await _roleManager.CreateAsync(new IdentityRole(uloga));

            var roleResult = await _userManager.AddToRoleAsync(user, uloga);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View("Index", await BuildViewModelAsync(null, "novi", novi, null));
            }

            TempData["Uspjeh"] = "Uposlenik je uspješno dodan.";
            return RedirectToAction(nameof(Index), new { area = "Admin", id = user.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UrediUposlenika([Bind(Prefix = "Odabrani")] UrediUposlenikForm odabrani)
        {
            var user = await _userManager.FindByIdAsync(odabrani.Id);
            if (user == null)
                return NotFound();

            var dijelovi = (odabrani.ImePrezime ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (dijelovi.Length < 2)
                ModelState.AddModelError("Odabrani.ImePrezime", "Unesite ime i prezime.");

            var uloga = string.IsNullOrWhiteSpace(odabrani.Uloga) ? "Uposlenik" : odabrani.Uloga.Trim();
            if (!DozvoljeneUloge.Contains(uloga))
                ModelState.AddModelError("Odabrani.Uloga", "Odabrana uloga nije validna.");

            var noviUsername = odabrani.Username.Trim();
            var postojeci = await _userManager.FindByNameAsync(noviUsername);
            if (postojeci != null && postojeci.Id != user.Id)
                ModelState.AddModelError("Odabrani.Username", "Username je već zauzet.");

            if (!ModelState.IsValid)
                return View("Index", await BuildViewModelAsync(odabrani.Id, "edit", new NoviUposlenikForm(), odabrani));

            user.Ime = dijelovi[0];
            user.Prezime = dijelovi[1];
            user.UserName = noviUsername;
            user.Email = $"{noviUsername}@etfpay.local";
            user.PhoneNumber = odabrani.BrojTelefona;
            user.Plata = odabrani.Plata;
            if (odabrani.DatumZaposlenja.HasValue)
                user.DatumZaposlenja = odabrani.DatumZaposlenja;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View("Index", await BuildViewModelAsync(odabrani.Id, "edit", new NoviUposlenikForm(), odabrani));
            }

            if (!string.IsNullOrWhiteSpace(odabrani.NovaSifra))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, odabrani.NovaSifra);
                if (!passResult.Succeeded)
                {
                    foreach (var error in passResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return View("Index", await BuildViewModelAsync(odabrani.Id, "edit", new NoviUposlenikForm(), odabrani));
                }
            }

            var stareUloge = await _userManager.GetRolesAsync(user);
            var ukloni = stareUloge.Where(r => DozvoljeneUloge.Contains(r)).ToList();
            if (ukloni.Any())
                await _userManager.RemoveFromRolesAsync(user, ukloni);

            if (!await _roleManager.RoleExistsAsync(uloga))
                await _roleManager.CreateAsync(new IdentityRole(uloga));

            await _userManager.AddToRoleAsync(user, uloga);

            TempData["Uspjeh"] = "Podaci uposlenika su izmijenjeni.";
            return RedirectToAction(nameof(Index), new { area = "Admin", id = user.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ObrisiUposlenika(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var trenutni = await _userManager.GetUserAsync(User);
            if (trenutni?.Id == user.Id)
            {
                TempData["Greska"] = "Ne možete obrisati vlastiti nalog.";
                return RedirectToAction(nameof(Index), new { area = "Admin", id });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Greska"] = "Brisanje nije uspjelo.";
                return RedirectToAction(nameof(Index), new { area = "Admin", id });
            }

            TempData["Uspjeh"] = "Uposlenik je obrisan.";
            return RedirectToAction(nameof(Index), new { area = "Admin" });
        }

        private async Task<AdminDashboardViewModel> BuildViewModelAsync(
            string? odabraniId,
            string? mode,
            NoviUposlenikForm novi,
            UrediUposlenikForm? odabraniOverride)
        {
            var svi = new List<Osoba>();
            foreach (var uloga in DozvoljeneUloge)
            {
                var korisnici = await _userManager.GetUsersInRoleAsync(uloga);
                foreach (var k in korisnici)
                {
                    if (svi.All(x => x.Id != k.Id))
                        svi.Add(k);
                }
            }

            svi = svi.OrderBy(o => o.Prezime).ThenBy(o => o.Ime).ToList();

            var items = new List<UposlenikListItem>();
            foreach (var o in svi)
            {
                var uloge = await _userManager.GetRolesAsync(o);
                var uloga = uloge.FirstOrDefault(r => DozvoljeneUloge.Contains(r));
                items.Add(new UposlenikListItem
                {
                    Id = o.Id,
                    PunoIme = $"{o.Ime} {o.Prezime}",
                    Uloga = PrikaziUlogu(uloga)
                });
            }

            var prikaziNovi = mode == "novi";
            var prikaziUredi = mode == "edit";
            string? resolvedId;
            UrediUposlenikForm? odabrani = odabraniOverride;

            if (prikaziNovi)
            {
                resolvedId = null;
                odabrani = null;
            }
            else
            {
                resolvedId = odabraniId ?? items.FirstOrDefault()?.Id;

                if (odabrani == null && !string.IsNullOrEmpty(resolvedId))
                {
                    var osoba = await _userManager.FindByIdAsync(resolvedId);
                    if (osoba != null)
                    {
                        var ulogaKod = (await _userManager.GetRolesAsync(osoba))
                            .FirstOrDefault(r => DozvoljeneUloge.Contains(r)) ?? "Uposlenik";

                        odabrani = new UrediUposlenikForm
                        {
                            Id = osoba.Id,
                            ImePrezime = $"{osoba.Ime} {osoba.Prezime}",
                            Uloga = ulogaKod,
                            DatumZaposlenja = osoba.DatumZaposlenja,
                            Plata = osoba.Plata,
                            BrojTelefona = osoba.PhoneNumber,
                            Username = osoba.UserName ?? ""
                        };
                    }
                }
            }

            return new AdminDashboardViewModel
            {
                Uposlenici = items,
                Novi = novi,
                OdabraniId = resolvedId,
                Odabrani = odabrani,
                PrikaziNoviForm = prikaziNovi,
                PrikaziUrediForm = prikaziUredi
            };
        }

        private async Task<UserSearchViewModel> BuildUserSearchViewModelAsync(string? q, string? filter, string? id)
        {
            var klijenti = await _userManager.GetUsersInRoleAsync("Client");
            var racuni = await _context.Racun.ToDictionaryAsync(r => r.Id);

            var items = new List<KlijentListItem>();
            foreach (var user in klijenti.OrderBy(u => u.Prezime).ThenBy(u => u.Ime))
            {
                racuni.TryGetValue(user.Racun ?? "", out var racun);
                items.Add(new KlijentListItem
                {
                    Id = user.Id,
                    PunoIme = $"{user.Ime} {user.Prezime}",
                    BrojRacuna = racun?.brojRacuna ?? "-",
                    Aktivan = racun?.Aktivan ?? false,
                    Stanje = racun?.Stanje ?? 0,
                    ImaTelefon = !string.IsNullOrWhiteSpace(user.PhoneNumber)
                });
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var upit = q.Trim().ToLower();
                items = items
                    .Where(k =>
                        k.PunoIme.ToLower().Contains(upit) ||
                        k.BrojRacuna.ToLower().Contains(upit))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(filter))
            {
                items = filter switch
                {
                    "aktivan" => items.Where(k => k.Aktivan).ToList(),
                    "neaktivan" => items.Where(k => !k.Aktivan && k.BrojRacuna != "-").ToList(),
                    "pozitivno-stanje" => items.Where(k => k.Stanje > 0).ToList(),
                    "nulto-stanje" => items.Where(k => k.Stanje == 0).ToList(),
                    "ima-telefon" => items.Where(k => k.ImaTelefon).ToList(),
                    "nema-telefon" => items.Where(k => !k.ImaTelefon).ToList(),
                    _ => items
                };
            }

            var resolvedId = !string.IsNullOrEmpty(id) && items.Any(k => k.Id == id)
                ? id
                : items.FirstOrDefault()?.Id;
            KlijentDetalji? odabrani = null;

            if (!string.IsNullOrEmpty(resolvedId))
            {
                var user = klijenti.FirstOrDefault(k => k.Id == resolvedId);
                if (user != null)
                {
                    racuni.TryGetValue(user.Racun ?? "", out var racun);
                    odabrani = new KlijentDetalji
                    {
                        Id = user.Id,
                        ImePrezime = $"{user.Ime} {user.Prezime}",
                        Email = user.Email ?? "-",
                        BrojTelefona = user.PhoneNumber,
                        Jmbg = user.JMBG,
                        DatumRodjenja = user.DatumRodjenja,
                        BrojRacuna = racun?.brojRacuna ?? "-",
                        Iban = racun?.IBAN ?? "-",
                        Stanje = racun?.Stanje ?? 0,
                        Aktivan = racun?.Aktivan ?? false,
                        DatumKreiranjaRacuna = racun?.DatumKreiranja
                    };
                }
            }

            return new UserSearchViewModel
            {
                Klijenti = items,
                Pretraga = q,
                Filter = filter,
                OdabraniId = resolvedId,
                Odabrani = odabrani
            };
        }

        private static string PrikaziUlogu(string? uloga) =>
            uloga switch
            {
                "Uposlenik" => "Operater",
                "Zastitar" => "Zaštitar",
                "Direktor" => "Direktor",
                "Domar" => "Domar",
                "Blagajnik" => "Blagajnik",
                "Admin" => "Admin",
                _ => uloga ?? "-"
            };

        private static string GenerisiJmbg()
        {
            var random = new Random();
            return $"{random.Next(1, 29):D2}{random.Next(1, 13):D2}{random.Next(100, 999)}{random.Next(100000, 999999)}";
        }

        public class AdminDashboardViewModel
        {
            public List<UposlenikListItem> Uposlenici { get; set; } = new();
            public NoviUposlenikForm Novi { get; set; } = new();
            public UrediUposlenikForm? Odabrani { get; set; }
            public string? OdabraniId { get; set; }
            public bool PrikaziNoviForm { get; set; }
            public bool PrikaziUrediForm { get; set; }
        }

        public class UrediUposlenikForm
        {
            public string Id { get; set; } = "";

            [Required(ErrorMessage = "Ime i prezime je obavezno.")]
            public string ImePrezime { get; set; } = "";

            [Required(ErrorMessage = "Uloga je obavezna.")]
            public string Uloga { get; set; } = "Uposlenik";

            public DateOnly? DatumZaposlenja { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Plata mora biti pozitivan broj.")]
            public double? Plata { get; set; }

            public string? BrojTelefona { get; set; }

            [Required(ErrorMessage = "Username je obavezan.")]
            public string Username { get; set; } = "";

            [MinLength(6, ErrorMessage = "Šifra mora imati najmanje 6 znakova.")]
            [DataType(DataType.Password)]
            public string? NovaSifra { get; set; }
        }

        public class UposlenikListItem
        {
            public string Id { get; set; } = "";
            public string PunoIme { get; set; } = "";
            public string Uloga { get; set; } = "";
        }

        public class NoviUposlenikForm
        {
            [Required(ErrorMessage = "Ime i prezime je obavezno.")]
            public string ImePrezime { get; set; } = "";

            [Required(ErrorMessage = "Uloga je obavezna.")]
            public string Uloga { get; set; } = "Uposlenik";

            [Range(0, double.MaxValue, ErrorMessage = "Plata mora biti pozitivan broj.")]
            public double? Plata { get; set; }

            public string? BrojTelefona { get; set; }

            [Required(ErrorMessage = "Username je obavezan.")]
            public string Username { get; set; } = "";

            [Required(ErrorMessage = "Šifra je obavezna.")]
            [MinLength(6, ErrorMessage = "Šifra mora imati najmanje 6 znakova.")]
            [DataType(DataType.Password)]
            public string Sifra { get; set; } = "";
        }

        public class UserSearchViewModel
        {
            public List<KlijentListItem> Klijenti { get; set; } = new();
            public string? Pretraga { get; set; }
            public string? Filter { get; set; }
            public string? OdabraniId { get; set; }
            public KlijentDetalji? Odabrani { get; set; }
        }

        public class KlijentDetalji
        {
            public string Id { get; set; } = "";
            public string ImePrezime { get; set; } = "";
            public string Email { get; set; } = "";
            public string? BrojTelefona { get; set; }
            public string Jmbg { get; set; } = "";
            public DateOnly DatumRodjenja { get; set; }
            public string BrojRacuna { get; set; } = "";
            public string Iban { get; set; } = "";
            public double Stanje { get; set; }
            public bool Aktivan { get; set; }
            public DateOnly? DatumKreiranjaRacuna { get; set; }
        }

        public class KlijentListItem
        {
            public string Id { get; set; } = "";
            public string PunoIme { get; set; } = "";
            public string BrojRacuna { get; set; } = "";
            public bool Aktivan { get; set; }
            public double Stanje { get; set; }
            public bool ImaTelefon { get; set; }
        }
    }
}
