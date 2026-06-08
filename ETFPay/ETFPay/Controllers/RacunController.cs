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

namespace ETFPay.Controllers
{
    public class RacunController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RacunController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Racun
        public async Task<IActionResult> Index()
        {
            return View(await _context.Racun.ToListAsync());
        }

        // GET: Racun/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var racun = await _context.Racun
                .FirstOrDefaultAsync(m => m.Id == id);
            if (racun == null)
            {
                return NotFound();
            }

            return View(racun);
        }

        // GET: Racun/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Racun/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,brojRacuna,Stanje,DatumKreiranja,IBAN,Aktivan")] Racun racun)
        {
            if (ModelState.IsValid)
            {
                _context.Add(racun);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(racun);
        }

        // GET: Racun/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var racun = await _context.Racun.FindAsync(id);
            if (racun == null)
            {
                return NotFound();
            }
            return View(racun);
        }

        // POST: Racun/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,brojRacuna,Stanje,DatumKreiranja,IBAN,Aktivan")] Racun racun)
        {
            if (id != racun.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(racun);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RacunExists(racun.Id))
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
            return View(racun);
        }

        // GET: Racun/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var racun = await _context.Racun
                .FirstOrDefaultAsync(m => m.Id == id);
            if (racun == null)
            {
                return NotFound();
            }

            return View(racun);
        }

        // POST: Racun/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var racun = await _context.Racun.FindAsync(id);
            if (racun != null)
            {
                _context.Racun.Remove(racun);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RacunExists(string id)
        {
            return _context.Racun.Any(e => e.Id == id);
        }
    }
}
