using ETFPay.Data;
using ETFPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ETFPay.Controllers
{
    [Authorize(Roles ="Admin,Uposlenik")]
    public class TransakcijaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransakcijaController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Transakcija()
        {
            return View();
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
        public async Task<IActionResult> Create([Bind("Id,Primaoc,Posiljaoc,Iznos,VrijemeTransakcije")] Transakcija transakcija)
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
        public async Task<IActionResult> Edit(string id, [Bind("Id,Primaoc,Posiljaoc,Iznos,VrijemeTransakcije")] Transakcija transakcija)
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
        public class CreateTransactionRequest
        {
            public string RecipientAccountNumber { get; set; }
            public Double Amount { get; set; }
            public string? Purpose { get; set; }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTransaction(CreateTransactionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. uzmi user ID iz sesije (JWT/cookie identity)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 2. nađi usera u bazi
            var sender = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (sender == null)
                return Unauthorized();

            // 3. validacija iznosa
            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than 0");

            // 4. nađi sender account (ako imaš relaciju)
            var senderAccount = await _context.Racun
     .FirstOrDefaultAsync(r => r.Osoba.Id == userId);

            if (senderAccount == null)
                return BadRequest("Sender account not found");

            // 5. nađi recipient account
            var recipientAccount = await _context.Racun
                .FirstOrDefaultAsync(a => a.brojRacuna == request.RecipientAccountNumber);

            if (recipientAccount == null)
                return BadRequest("Recipient account not found");

            // 6. provjera balance-a
            if (senderAccount.Stanje < request.Amount)
                return BadRequest("Insufficient funds");

            // 7. izvrši transakciju
            senderAccount.Stanje -= request.Amount;
            recipientAccount.Stanje += request.Amount;

            var transaction = new Transakcija
            {

                Posiljaoc = senderAccount.Id,
                Primaoc = recipientAccount.Id,
                Iznos = request.Amount,
                SvrhaUplate=request.Purpose,
                VrijemeTransakcije = DateTime.UtcNow
            };

            _context.Transakcija.Add(transaction);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Transaction successful" });
        }

    }
}
