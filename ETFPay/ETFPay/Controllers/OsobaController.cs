using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ETFPay.Data;
using ETFPay.Models;
using Microsoft.EntityFrameworkCore;

namespace ETFPay.Controllers
{
    [Authorize]
    public class OsobaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OsobaController(ApplicationDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> ProcessZahtjev(string userId, string action)
        {
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("ZahtjeviZaRacun");

            var osoba = await _context.Users
                .Include(u => u.RacunKorisnika)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (osoba == null)
                return RedirectToAction("ZahtjeviZaRacun");

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
                    {
                        _context.Racun.Remove(osoba.RacunKorisnika);
                    }

                    _context.Users.Remove(osoba);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex) { }

            return RedirectToAction("ZahtjeviZaRacun");
        }
    }
}
