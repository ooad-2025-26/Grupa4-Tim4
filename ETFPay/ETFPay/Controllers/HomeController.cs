using ETFPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ETFPay.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                // 2. Ako je Admin, pošalji ga na Admin Dashboard
                if (User.IsInRole("Administrator"))
                {
                    return RedirectToAction("AdminIndex", "Home");
                }

                // 3. Ako je Client, pošalji ga na njegovu početnu
                if (User.IsInRole("Client"))
                {
                    return RedirectToAction("ClientIndex", "Home");
                }
            }
            return View();
        }
        // Početna za Admina (Pristup dopušten samo Administratorima)
        [Authorize(Roles = "Administrator")]
        public IActionResult AdminIndex()
        {
            return View();
        }

        [Authorize(Roles = "Client")]
        public IActionResult ClientIndex()
        {
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
    }
}
