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

        // GET: Predlozak
        [Authorize(Roles = "Admin,Uposlenik")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Predlozak.ToListAsync());
        }

        // GET: Predlozak/PretplataView
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> PretplataView(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var subscriptions = await _context.Predlozak
                .Where(p => p.Pretplata == true && p.BrojRacuna == currentUser.Racun)
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

        // GET: Predlozak/DodavanjePretplate
        [Authorize(Roles = "Client")]
        public IActionResult DodavanjePretplate()
        {
            return View();
        }

        // POST: Predlozak/DodavanjePretplate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> DodavanjePretplate([Bind("Naziv,Primaoc,SvrhaUplate,Adresa,Grad,BrojRacuna,Iznos,Period")] Predlozak predlozak)
        {
            ModelState.Remove("Id");
            ModelState.Remove("Pretplata");

            if (string.IsNullOrEmpty(Request.Form["Period"]))
            {
                ModelState.Remove("Period");
                predlozak.Period = Period.Mjesecno;
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        return Unauthorized();
                    }

                    predlozak.Id = Guid.NewGuid().ToString();
                    predlozak.Pretplata = true;
                    predlozak.BrojRacuna = currentUser.Racun;

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

        // GET: Predlozak/PredlozakView
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> PredlozakView(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var templates = await _context.Predlozak
                .Where(p => p.Pretplata == false && p.BrojRacuna == currentUser.Racun)
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

        // GET: Predlozak/DodavanjePredlozaka
        [Authorize(Roles = "Client")]
        public IActionResult DodavanjePredlozaka()
        {
            return View();
        }

        // POST: Predlozak/DodavanjePredlozaka
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> DodavanjePredlozaka([Bind("Naziv,Primaoc,SvrhaUplate,Adresa,Grad,BrojRacuna,Iznos")] Predlozak predlozak)
        {
            ModelState.Remove("Id");
            ModelState.Remove("Pretplata");
            ModelState.Remove("Period");

            try
            {
                if (ModelState.IsValid)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        return Unauthorized();
                    }

                    predlozak.Id = Guid.NewGuid().ToString();
                    predlozak.Pretplata = false;
                    predlozak.BrojRacuna = currentUser.Racun;

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

        // GET: Predlozak/Details/5
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

        // GET: Predlozak/Create
        [Authorize(Roles = "Admin,Uposlenik")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Predlozak/Create
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

        // GET: Predlozak/Edit/5
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

        // POST: Predlozak/Edit/5
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

        // GET: Predlozak/Delete/5
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

        // POST: Predlozak/Delete/5
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
                return BadRequest("Podaci nisu ispravno poslani.");
            }

            ModelState.Remove("Id");
            ModelState.Remove("Pretplata");
            ModelState.Remove("Period");

            try
            {
                if (ModelState.IsValid)
                {
                    predlozak.Id = Guid.NewGuid().ToString();
                    predlozak.Pretplata = false;

                    _context.Add(predlozak);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Predložak uspješno spasen u bazu!" });
                }

                return BadRequest("Podaci nisu validni.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Greška prilikom spasavanja u bazu: " + ex.Message);
            }
        }

        private bool PredlozakExists(string id)
        {
            return _context.Predlozak.Any(e => e.Id == id);
        }
    }
}