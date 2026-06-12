using ETFPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ETFPay.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace ETFPay.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<Osoba> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(UserManager<Osoba> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                // 2. Ako je Admin, pošalji ga na Admin Dashboard
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("AdminIndex", "Home");
                }

                // 3. Ako je Client, pošalji ga na njegovu početnu
                if (User.IsInRole("Client"))
                {
                    return RedirectToAction("ClientIndex", "Home");
                }

                if (User.IsInRole("Uposlenik"))
                {
                    return RedirectToAction("UposlenikIndex", "Home");
                }
            }
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminIndex()
        {
            return View();
        }

        [Authorize(Roles = "Uposlenik")]
        public IActionResult UposlenikIndex()
        {
            return View();
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> ClientIndex() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var racun = await _context.Racun.FirstOrDefaultAsync(r => r.Id == user.Racun);
            ViewBag.RacunAktivan = racun?.Aktivan == true;
            ViewBag.Racun = racun;

            if (racun?.Aktivan == true)
            {
                ViewBag.ZadnjeTransakcije = await _context.Transakcija
                    .Where(t => t.Posiljaoc == racun.Id || t.Primaoc == racun.Id)
                    .OrderByDescending(t => t.VrijemeTransakcije)
                    .Take(3)
                    .ToListAsync();
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("Error/404")]
        public IActionResult PageNotFound()
        {
            return View("NotFound");
        }
    }
}
