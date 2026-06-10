using ETFPay.Data;
using ETFPay.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ETFPay.Filters
{
    public class ClientAccountViewBagFilter : IAsyncActionFilter
    {
        private readonly UserManager<Osoba> _userManager;
        private readonly ApplicationDbContext _context;

        public ClientAccountViewBagFilter(UserManager<Osoba> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.IsInRole("Client") && context.Controller is Microsoft.AspNetCore.Mvc.Controller controller)
            {
                var user = await _userManager.GetUserAsync(context.HttpContext.User);
                if (user != null && !string.IsNullOrEmpty(user.Racun))
                {
                    var racun = await _context.Racun.AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == user.Racun);
                    controller.ViewBag.RacunAktivan = racun?.Aktivan == true;
                    controller.ViewBag.Racun = racun;
                }
                else
                {
                    controller.ViewBag.RacunAktivan = false;
                    controller.ViewBag.Racun = null;
                }
            }

            await next();
        }
    }
}
