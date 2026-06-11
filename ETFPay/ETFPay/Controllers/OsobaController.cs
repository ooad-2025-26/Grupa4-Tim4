using System.ComponentModel.DataAnnotations;
using ETFPay.Data;
using ETFPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ETFPay.Controllers
{
    [Authorize]
    public class OsobaController : Controller
    {
        private static readonly string[] DozvoljeneUloge =
            { "Uposlenik", "Zastitar", "Direktor", "Domar", "Blagajnik", "Admin" };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<Osoba> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public OsobaController(
            ApplicationDbContext context,
            UserManager<Osoba> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize(Roles = "Uposlenik")]
        public async Task<IActionResult> ZahtjeviZaRacun(string? selectedUserId = null)
        {
            var zahtjevi = await _context.Users
                .Include(u => u.RacunKorisnika)
                .Where(u => u.RacunKorisnika != null && !u.RacunKorisnika.Aktivan)
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var selectedUser = selectedUserId != null
                ? zahtjevi.FirstOrDefault(u => u.Id == selectedUserId)
                : null;

            ViewData["SelectedUserId"] = selectedUserId;
            ViewData["SelectedUser"] = selectedUser;

            return View("ZahtjeviZaRacunView", zahtjevi);
        }

        [Authorize(Roles = "Uposlenik")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessZahtjev(string userId, string action)
        {
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction(nameof(ZahtjeviZaRacun));

            var osoba = await _context.Users
                .Include(u => u.RacunKorisnika)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (osoba == null)
                return RedirectToAction(nameof(ZahtjeviZaRacun));

            try
            {
                if (action == "approve")
                {
                    if (osoba.RacunKorisnika != null)
                    {
                        osoba.RacunKorisnika.Aktivan = true;
                        _context.Racun.Update(osoba.RacunKorisnika);
                        await _context.SaveChangesAsync();
                    }
                }
                else if (action == "delete")
                {
                    if (osoba.RacunKorisnika != null)
                        _context.Racun.Remove(osoba.RacunKorisnika);

                    _context.Users.Remove(osoba);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
            }

            return RedirectToAction(nameof(ZahtjeviZaRacun));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string? id, string? mode)
        {
            return View(await BuildViewModelAsync(id, mode, new NoviUposlenikForm(), null));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Uposlenik")]
        public async Task<IActionResult> UserSearch(string? q, string? filter, string? sort, string? sortDir, string? id)
        {
            return View(await BuildUserSearchViewModelAsync(q, filter, sort, sortDir, id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> KreirajUposlenika([Bind(Prefix = "Novi")] NoviUposlenikForm novi)
        {
            var dijelovi = (novi.ImePrezime ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (dijelovi.Length < 2)
                ModelState.AddModelError("Novi.ImePrezime", "Enter first and last name.");

            var uloga = string.IsNullOrWhiteSpace(novi.Uloga) ? "Uposlenik" : novi.Uloga.Trim();
            if (!DozvoljeneUloge.Contains(uloga))
                ModelState.AddModelError("Novi.Uloga", "Selected role is not valid.");

            if (!ModelState.IsValid)
                return View("Index", await BuildViewModelAsync(null, "novi", novi, null));

            var username = novi.Username.Trim();
            if (await _userManager.FindByNameAsync(username) != null)
            {
                ModelState.AddModelError("Novi.Username", "Username is already taken.");
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

            TempData["Success"] = "Employee added successfully.";
            return RedirectToAction(nameof(Index), new { id = user.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UrediUposlenika([Bind(Prefix = "Odabrani")] UrediUposlenikForm odabrani)
        {
            var user = await _userManager.FindByIdAsync(odabrani.Id);
            if (user == null)
                return NotFound();

            var dijelovi = (odabrani.ImePrezime ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (dijelovi.Length < 2)
                ModelState.AddModelError("Odabrani.ImePrezime", "Enter first and last name.");

            var uloga = string.IsNullOrWhiteSpace(odabrani.Uloga) ? "Uposlenik" : odabrani.Uloga.Trim();
            if (!DozvoljeneUloge.Contains(uloga))
                ModelState.AddModelError("Odabrani.Uloga", "Selected role is not valid.");

            var noviUsername = odabrani.Username.Trim();
            var postojeci = await _userManager.FindByNameAsync(noviUsername);
            if (postojeci != null && postojeci.Id != user.Id)
                ModelState.AddModelError("Odabrani.Username", "Username is already taken.");

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

            TempData["Success"] = "Employee updated successfully.";
            return RedirectToAction(nameof(Index), new { id = user.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ObrisiUposlenika(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var trenutni = await _userManager.GetUserAsync(User);
            if (trenutni?.Id == user.Id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index), new { id });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Delete failed.";
                return RedirectToAction(nameof(Index), new { id });
            }

            TempData["Success"] = "Employee deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Uposlenik")]
        public async Task<IActionResult> ObrisiKlijenta(string id, string? q, string? filter, string? sort, string? sortDir)
        {
            var redirectParams = new { q, filter, sort, sortDir };

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "Client"))
            {
                TempData["Error"] = "Only client accounts can be deleted from this page.";
                return RedirectToAction(nameof(UserSearch), redirectParams);
            }

            var trenutni = await _userManager.GetUserAsync(User);
            if (trenutni?.Id == user.Id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(UserSearch), new { q, filter, sort, sortDir, id });
            }

            if (!string.IsNullOrEmpty(user.Racun))
            {
                var racunId = user.Racun;
                var transakcije = await _context.Transakcija
                    .Where(t => t.Primaoc == racunId || t.Posiljaoc == racunId)
                    .ToListAsync();
                _context.Transakcija.RemoveRange(transakcije);

                var racun = await _context.Racun.FindAsync(racunId);
                if (racun != null)
                    _context.Racun.Remove(racun);

                await _context.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Delete failed.";
                return RedirectToAction(nameof(UserSearch), new { q, filter, sort, sortDir, id });
            }

            TempData["Success"] = "Client deleted successfully.";
            return RedirectToAction(nameof(UserSearch), redirectParams);
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

        private async Task<UserSearchViewModel> BuildUserSearchViewModelAsync(string? q, string? filter, string? sort, string? sortDir, string? id)
        {
            var klijenti = await _userManager.GetUsersInRoleAsync("Client");
            var racuni = await _context.Racun.ToDictionaryAsync(r => r.Id);

            var items = new List<KlijentListItem>();
            foreach (var user in klijenti)
            {
                racuni.TryGetValue(user.Racun ?? "", out var racun);
                items.Add(new KlijentListItem
                {
                    Id = user.Id,
                    Ime = user.Ime,
                    Prezime = user.Prezime,
                    PunoIme = $"{user.Ime} {user.Prezime}",
                    BrojRacuna = racun?.brojRacuna ?? "-",
                    Aktivan = racun?.Aktivan ?? false,
                    Stanje = racun?.Stanje ?? 0
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
                    _ => items
                };
            }

            var sortKriterij = string.IsNullOrWhiteSpace(sort) ? "prezime" : sort.Trim().ToLower();
            var sortAscending = !string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            if (sortKriterij is "ime" or "prezime" or "stanje")
                QuickSortKlijente(items, sortKriterij, sortAscending);

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
                Sort = sortKriterij,
                SortDirection = sortAscending ? "asc" : "desc",
                OdabraniId = resolvedId,
                Odabrani = odabrani
            };
        }

        private static void QuickSortKlijente(List<KlijentListItem> items, string sortBy, bool ascending)
        {
            if (items.Count <= 1)
                return;

            QuickSortKlijente(items, 0, items.Count - 1, sortBy, ascending);
        }

        private static void QuickSortKlijente(List<KlijentListItem> items, int low, int high, string sortBy, bool ascending)
        {
            if (low >= high)
                return;

            var pivotIndex = PartitionKlijente(items, low, high, sortBy, ascending);
            QuickSortKlijente(items, low, pivotIndex - 1, sortBy, ascending);
            QuickSortKlijente(items, pivotIndex + 1, high, sortBy, ascending);
        }

        private static int PartitionKlijente(List<KlijentListItem> items, int low, int high, string sortBy, bool ascending)
        {
            var pivot = items[high];
            var i = low - 1;

            for (var j = low; j < high; j++)
            {
                if (UporediKlijente(items[j], pivot, sortBy, ascending) <= 0)
                {
                    i++;
                    (items[i], items[j]) = (items[j], items[i]);
                }
            }

            (items[i + 1], items[high]) = (items[high], items[i + 1]);
            return i + 1;
        }

        private static int UporediKlijente(KlijentListItem a, KlijentListItem b, string sortBy, bool ascending)
        {
            var result = sortBy switch
            {
                "ime" => string.Compare(a.Ime, b.Ime, StringComparison.OrdinalIgnoreCase),
                "stanje" => a.Stanje.CompareTo(b.Stanje),
                _ => string.Compare(a.Prezime, b.Prezime, StringComparison.OrdinalIgnoreCase)
            };

            return ascending ? result : -result;
        }

        private static string PrikaziUlogu(string? uloga) =>
            uloga switch
            {
                "Uposlenik" => "Employee",
                "Zastitar" => "Security",
                "Direktor" => "Director",
                "Domar" => "Janitor",
                "Blagajnik" => "Cashier",
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

            [Required(ErrorMessage = "Full name is required.")]
            public string ImePrezime { get; set; } = "";

            [Required(ErrorMessage = "Role is required.")]
            public string Uloga { get; set; } = "Uposlenik";

            public DateOnly? DatumZaposlenja { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number.")]
            public double? Plata { get; set; }

            public string? BrojTelefona { get; set; }

            [Required(ErrorMessage = "Username is required.")]
            public string Username { get; set; } = "";

            [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
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
            [Required(ErrorMessage = "Full name is required.")]
            public string ImePrezime { get; set; } = "";

            [Required(ErrorMessage = "Role is required.")]
            public string Uloga { get; set; } = "Uposlenik";

            [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number.")]
            public double? Plata { get; set; }

            public string? BrojTelefona { get; set; }

            [Required(ErrorMessage = "Username is required.")]
            public string Username { get; set; } = "";

            [Required(ErrorMessage = "Password is required.")]
            [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
            [DataType(DataType.Password)]
            public string Sifra { get; set; } = "";
        }

        public class UserSearchViewModel
        {
            public List<KlijentListItem> Klijenti { get; set; } = new();
            public string? Pretraga { get; set; }
            public string? Filter { get; set; }
            public string Sort { get; set; } = "prezime";
            public string SortDirection { get; set; } = "asc";
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
            public string Ime { get; set; } = "";
            public string Prezime { get; set; } = "";
            public string PunoIme { get; set; } = "";
            public string BrojRacuna { get; set; } = "";
            public bool Aktivan { get; set; }
            public double Stanje { get; set; }
        }
    }
}
