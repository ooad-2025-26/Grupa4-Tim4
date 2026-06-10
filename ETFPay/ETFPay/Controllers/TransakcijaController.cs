using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ETFPay.Data;
using ETFPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
namespace ETFPay.Controllers
{
    public class TransakcijaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransakcijaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Transakcija
        public async Task<IActionResult> Index()
        {
            return View(await _context.Transakcija.ToListAsync());
        }

        // GET: Transakcija/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transakcija = await _context.Transakcija
                .FirstOrDefaultAsync(m => m.Id == id);
            if (transakcija == null)
            {
                return NotFound();
            }

            return View(transakcija);
        }

        // GET: Transakcija/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Transakcija/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Primaoc,Posiljaoc,Iznos,VrijemeTransakcije,SvrhaUplate")] Transakcija transakcija)
        {
            if (ModelState.IsValid)
            {
                _context.Add(transakcija);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(transakcija);
        }

        // GET: Transakcija/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transakcija = await _context.Transakcija.FindAsync(id);
            if (transakcija == null)
            {
                return NotFound();
            }
            return View(transakcija);
        }

        // POST: Transakcija/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Primaoc,Posiljaoc,Iznos,VrijemeTransakcije,SvrhaUplate")] Transakcija transakcija)
        {
            if (id != transakcija.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(transakcija);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransakcijaExists(transakcija.Id))
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
            return View(transakcija);
        }

        // GET: Transakcija/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transakcija = await _context.Transakcija
                .FirstOrDefaultAsync(m => m.Id == id);
            if (transakcija == null)
            {
                return NotFound();
            }

            return View(transakcija);
        }

        // POST: Transakcija/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var transakcija = await _context.Transakcija.FindAsync(id);
            if (transakcija != null)
            {
                _context.Transakcija.Remove(transakcija);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TransakcijaExists(string id)
        {
            return _context.Transakcija.Any(e => e.Id == id);
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> MojeTransakcije(string? id, [FromServices] UserManager<Osoba> userManager)
        {

            var user = await userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrEmpty(user.Racun)) return RedirectToAction("ClientIndex", "Home");
            var brojRacuna = await _context.Racun
                .Where(r => r.Id == user.Racun)
                .Select(r => r.brojRacuna)
                .FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(brojRacuna)) return RedirectToAction("ClientIndex", "Home");

            var transakcije = await _context.Transakcija
                .Where(t => t.Primaoc == user.Racun || t.Posiljaoc == user.Racun)
                .OrderByDescending(t => t.VrijemeTransakcije)
                .ToListAsync();
            return View(new TransakcijeViewModel
            {
                Transakcije = transakcije,
                Odabrana = !string.IsNullOrEmpty(id) ? transakcije.FirstOrDefault(t => t.Id == id) : transakcije.FirstOrDefault(),
                BrojRacunaKorisnika = brojRacuna,
                RacunIdKorisnika = user.Racun
            });


        }
    }
}
