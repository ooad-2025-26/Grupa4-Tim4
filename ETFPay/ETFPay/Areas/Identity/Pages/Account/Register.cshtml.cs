// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using ETFPay.Data;
using ETFPay.Models;

namespace ETFPay.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<Osoba> _signInManager;
    private readonly UserManager<Osoba> _userManager;
    private readonly IUserStore<Osoba> _userStore;
    private readonly IUserEmailStore<Osoba> _emailStore;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public RegisterModel(
        UserManager<Osoba> userManager,
        IUserStore<Osoba> userStore,
        SignInManager<Osoba> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
        _roleManager = roleManager;
        _context = context;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = default!;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = default!;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }


        [Required(ErrorMessage = "Ime je obavezno.")]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Prezime je obavezno.")]
        public string Prezime { get; set; }

        [Required(ErrorMessage = "Broj telefona je obavezan.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "JMBG je obavezan.")]
        public string JMBG { get; set; }

        [Required(ErrorMessage = "Datum rođenja je obavezan.")]
        public DateTime DatumRodjenja { get; set; }


    }


    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        if (ModelState.IsValid)
        {
            var user = CreateUser();
         
            user.Ime = Input.Ime;
            user.Prezime = Input.Prezime;
            user.PhoneNumber = Input.PhoneNumber;
            user.JMBG = Input.JMBG;
            user.DatumRodjenja = DateOnly.FromDateTime(Input.DatumRodjenja);

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                string defaultRole = "Client";

                // Provjera postoji li rola u bazi; ako ne postoji, kreira je da kod ne pukne
                if (!await _roleManager.RoleExistsAsync(defaultRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(defaultRole));
                }
                await _userManager.AddToRoleAsync(user, defaultRole);

                try
                {
                    string noviBrojRacuna;
                    bool zauzet;
                
                    do
                    {
                        noviBrojRacuna = GenerisiValidanBrojRacuna();
                        zauzet = _context.Racun.Any(r => r.brojRacuna == noviBrojRacuna);
                    } while (zauzet);
                    string noviIban = GenerateValidBosnianIBAN(noviBrojRacuna);
                    var noviRacun = new Racun
                    {
                        Id = Guid.NewGuid().ToString(),
                        brojRacuna = noviBrojRacuna,
                        IBAN = noviIban,
                        Stanje = 0,
                        DatumKreiranja = DateOnly.FromDateTime(DateTime.Now),
                        Aktivan = false
                    };
                    _context.Racun.Add(noviRacun);
                    await _context.SaveChangesAsync();
                    user.Racun = noviRacun.Id;
                    await _userManager.UpdateAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Greška prilikom kreiranja računa pri registraciji korisnika: {ex.Message}");
                }


                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme)!;

                await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                }
                else
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }


        // If we got this far, something failed, redisplay form
        return Page();
    }

    private Osoba CreateUser()
    {
        try
        {
            return Activator.CreateInstance<Osoba>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(Osoba)}'. " +
                $"Ensure that '{nameof(Osoba)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
        }
    }

    private IUserEmailStore<Osoba> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }
        return (IUserEmailStore<Osoba>)_userStore;
    }

    private string GenerisiValidanBrojRacuna()
    {
        Random random = new Random();
        string kodPoslovnice = "000"; // Fiksni kod poslovnice

        string brojKlijenta = "";
        for (int i = 0; i < 8; i++)
        {
            brojKlijenta += random.Next(0, 10).ToString();
        }
        string kodBanke = "999"; // nasumicni kod banke u BiH
        
        string baza = kodBanke + kodPoslovnice + brojKlijenta;
        
        // modulo 97 algoritam za kontrolne cifre
        long bazaLong = long.Parse(baza) * 100;
        int ostatak = (int)(bazaLong % 97);
        int kontrolni = 98 - ostatak;

        return baza + kontrolni.ToString("D2");
    }

    private string GenerateValidBosnianIBAN(string accountNumber)
    {
        
        // B = 11, A = 10 -> BA00 postaje 111000
        string broj = accountNumber + "111000";
        
        System.Numerics.BigInteger bigNum = System.Numerics.BigInteger.Parse(broj);
        int ostatak = (int)(bigNum % 97);
        int controlValue = 98 - ostatak;
        string kk = controlValue.ToString("D2");
        return "BA" + kk + accountNumber;
    }

}
