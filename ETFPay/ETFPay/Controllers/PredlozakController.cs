using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ETFPay.Data;
using ETFPay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ETFPay.Controllers
{
    public class PredlozakController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Osoba> _userManager;

        public PredlozakController(ApplicationDbContext context, UserManager<Osoba> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin,Uposlenik")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Predlozak.ToListAsync());
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> PretplataView(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userWithAccount = await _context.Users
                .Include(u => u.RacunKorisnika)
                .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

            if (userWithAccount?.RacunKorisnika == null)
            {
                return BadRequest("User account information is missing.");
            }

            var subscriptions = await _context.Predlozak
                .Where(p => p.Pretplata == true && p.BrojRacuna == userWithAccount.RacunKorisnika.brojRacuna)
                .ToListAsync();

            if (!subscriptions.Any())
            {
                return View(new List<Predlozak>());
            }

            var selectedSubscription = id != null
                ? subscriptions.FirstOrDefault(s => s.Id == id)
                : subscriptions.FirstOrDefault();

            if (selectedSubscription != null)
            {
                ViewBag.SelectedSubscription = selectedSubscription;
            }

            return View(subscriptions);
        }

        [Authorize(Roles = "Client")]
        public IActionResult DodavanjePretplate()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> DodavanjePretplate([Bind("Naziv,Primaoc,SvrhaUplate,Adresa,Grad,Iznos,Period")] Predlozak predlozak)
        {
            ModelState.Remove("Id");
            ModelState.Remove("Pretplata");
            ModelState.Remove("BrojRacuna");

            if (string.IsNullOrEmpty(Request.Form["Period"]))
            {
                ModelState.Remove("Period");
                predlozak.Period = Period.Mjesecno;
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var userWithAccount = await _context.Users
                    .Include(u => u.RacunKorisnika)
                    .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

                if (userWithAccount?.RacunKorisnika == null)
                {
                    return BadRequest("User account information is missing.");
                }

                if (predlozak.Primaoc == userWithAccount.RacunKorisnika.brojRacuna)
                {
                    ModelState.AddModelError("Primaoc", "You cannot create a subscription to pay to yourself.");
                }

                if (ModelState.IsValid)
                {
                    predlozak.Id = Guid.NewGuid().ToString();
                    predlozak.Pretplata = true;
                    predlozak.BrojRacuna = userWithAccount.RacunKorisnika.brojRacuna;

                    _context.Add(predlozak);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(PretplataView));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
            }

            return View(predlozak);
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> PredlozakView(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userWithAccount = await _context.Users
                .Include(u => u.RacunKorisnika)
                .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

            if (userWithAccount?.RacunKorisnika == null)
            {
                return BadRequest("User account information is missing.");
            }

            var templates = await _context.Predlozak
                .Where(p => p.Pretplata == false && p.BrojRacuna == userWithAccount.RacunKorisnika.brojRacuna)
                .ToListAsync();

            if (!templates.Any())
            {
                return View(new List<Predlozak>());
            }

            var selectedTemplate = id != null
                ? templates.FirstOrDefault(t => t.Id == id)
                : templates.FirstOrDefault();

            if (selectedTemplate != null)
            {
                ViewBag.SelectedTemplate = selectedTemplate;
            }

            return View(templates);
        }

        [Authorize(Roles = "Client")]
        public IActionResult DodavanjePredlozaka()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> DodavanjePredlozaka([Bind("Naziv,Primaoc,SvrhaUplate,Adresa,Grad,Iznos")] Predlozak predlozak)
        {
            ModelState.Remove("Id");
            ModelState.Remove("Pretplata");
            ModelState.Remove("Period");
            ModelState.Remove("BrojRacuna");

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var userWithAccount = await _context.Users
                    .Include(u => u.RacunKorisnika)
                    .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

                if (userWithAccount?.RacunKorisnika == null)
                {
                    return BadRequest("User account information is missing.");
                }

                if (predlozak.Primaoc == userWithAccount.RacunKorisnika.brojRacuna)
                {
                    ModelState.AddModelError("Primaoc", "You cannot create a template to pay to yourself.");
                }

                if (ModelState.IsValid)
                {
                    predlozak.Id = Guid.NewGuid().ToString();
                    predlozak.Pretplata = false;
                    predlozak.BrojRacuna = userWithAccount.RacunKorisnika.brojRacuna;

                    _context.Add(predlozak);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(PredlozakView));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
            }

            return View(predlozak);
        }

        [Authorize(Roles = "Admin,Uposlenik")]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var predlozak = await _context.Predlozak
                .FirstOrDefaultAsync(m => m.Id == id);
            if (predlozak == null)
            {
                return NotFound();
            }

            return View(predlozak);
        }

        [Authorize(Roles = "Admin,Uposlenik")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Uposlenik")]
        public async Task<IActionResult> Create([Bind("Id,Naziv,Primaoc,SvrhaUplate,Adresa,Grad,BrojRacuna,Iznos,Pretplata,Period")] Predlozak predlozak)
        {
            if (ModelState.IsValid)
            {
                _context.Add(predlozak);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(predlozak);
        }

        [Authorize(Roles = "Admin,Uposlenik")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var predlozak = await _context.Predlozak.FindAsync(id);
            if (predlozak == null)
            {
                return NotFound();
            }
            return View(predlozak);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Uposlenik")]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Naziv,Primaoc,SvrhaUplate,Adresa,Grad,BrojRacuna,Iznos,Pretplata,Period")] Predlozak predlozak)
        {
            if (id != predlozak.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(predlozak);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PredlozakExists(predlozak.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(predlozak);
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var predlozak = await _context.Predlozak
                .FirstOrDefaultAsync(m => m.Id == id);
            if (predlozak == null)
            {
                return NotFound();
            }

            return View(predlozak);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var predlozak = await _context.Predlozak.FindAsync(id);
            if (predlozak != null)
            {
                _context.Predlozak.Remove(predlozak);
                await _context.SaveChangesAsync();

                if (predlozak.Pretplata)
                {
                    return RedirectToAction(nameof(PretplataView));
                }
                else
                {
                    return RedirectToAction(nameof(PredlozakView));
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> SaveTemplate([FromBody] Predlozak predlozak)
        {
            if (predlozak == null)
            {
                return BadRequest(new { message = "Podaci nisu ispravno poslani." });
            }

            ModelState.Remove("Id");
            ModelState.Remove("Pretplata");
            ModelState.Remove("Period");
            ModelState.Remove("BrojRacuna");
            ModelState.Remove("Iznos");
            ModelState.Remove("PosljednjePlacanje");

            if (string.IsNullOrWhiteSpace(predlozak.Grad))
            {
                ModelState.Remove("Grad");
                predlozak.Grad = "";
            }

            if (string.IsNullOrWhiteSpace(predlozak.Adresa))
                predlozak.Adresa = "";

            try
            {
                if (ModelState.IsValid)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        return Unauthorized();
                    }

                    var userWithAccount = await _context.Users
                        .Include(u => u.RacunKorisnika)
                        .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

                    if (userWithAccount?.RacunKorisnika == null)
                    {
                        return BadRequest(new { message = "User account information is missing." });
                    }

                    predlozak.Id = Guid.NewGuid().ToString();
                    predlozak.Pretplata = false;
                    predlozak.BrojRacuna = userWithAccount.RacunKorisnika.brojRacuna;

                    _context.Add(predlozak);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Predložak uspješno spasen u bazu!" });
                }

                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .ToList();

                return BadRequest(new
                {
                    message = errors.FirstOrDefault() ?? "Podaci nisu validni."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Greška prilikom spasavanja u bazu: " + ex.Message });
            }
        }

        private bool PredlozakExists(string id)
        {
            return _context.Predlozak.Any(e => e.Id == id);
        }
    }
}