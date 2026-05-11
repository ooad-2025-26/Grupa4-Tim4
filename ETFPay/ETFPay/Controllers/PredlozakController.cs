using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ETFPay.Data;
using ETFPay.Models;

namespace ETFPay.Controllers
{
    public class PredlozakController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PredlozakController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Predlozak
        public async Task<IActionResult> Index()
        {
            return View(await _context.Predlozak.ToListAsync());
        }

        // GET: Predlozak/Details/5
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
        public IActionResult Create()
        {
            return View();
        }

        // POST: Predlozak/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var predlozak = await _context.Predlozak.FindAsync(id);
            if (predlozak != null)
            {
                _context.Predlozak.Remove(predlozak);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PredlozakExists(string id)
        {
            return _context.Predlozak.Any(e => e.Id == id);
        }
    }
}
