// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public RegisterModel(
        UserManager<Osoba> userManager,
        IUserStore<Osoba> userStore,
        SignInManager<Osoba> signInManager,
        ILogger<RegisterModel> logger,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        _roleManager = roleManager;
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Enter a valid email address (e.g. name@example.com).")]
        [Display(Name = "Email")]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = default!;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 20 characters long.")]
        [RegularExpression(@"^[a-zA-ZčćšđžČĆŠĐŽа-яА-Я]+$", ErrorMessage = "First name can only contain letters.")]
        [Display(Name = "First Name")]
        public string Ime { get; set; } = default!;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 30 characters long.")]
        [RegularExpression(@"^[a-zA-ZčćšđžČĆŠĐŽа-яА-Я\s-]+$", ErrorMessage = "Last name can only contain letters, spaces, or hyphens.")]
        [Display(Name = "Last Name")]
        public string Prezime { get; set; } = default!;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^0[0-9]{8,9}$", ErrorMessage = "Phone number must start with 0 and contain exactly 9 or 10 digits without any spaces or characters.")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = default!;
        

        [Required(ErrorMessage = "JMBG is required.")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "JMBG must be exactly 13 digits long.")]
        [RegularExpression(@"^(0[1-9]|[12][0-9]|3[01])(0[1-9]|1[012])[0-9]{3}[0-9]{6}$", ErrorMessage = "Invalid JMBG format.")]
        [Display(Name = "JMBG")]
        public string JMBG { get; set; } = default!;

        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DatumRodjenja { get; set; }
     
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        Input = new InputModel
        {
            DatumRodjenja = DateTime.Today.AddYears(-18)
        };
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (ModelState.IsValid)
        {
            var birthDateOnly = DateOnly.FromDateTime(Input.DatumRodjenja);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - birthDateOnly.Year;
            if (birthDateOnly > today.AddYears(-age)) age--;

            if (age < 16)
            {
                ModelState.AddModelError("Input.DatumRodjenja", "You must be at least 16 years old.");
                return Page();
            }

            if (!ValidajJmbgIDatum())
            {
                ModelState.AddModelError("Input.JMBG", "Date of birth does not match the JMBG data.");
                return Page();  
            }
            // Provera da li korisnik sa tim emailom već postoji u bazi
            var postojećiKorisnik = await _userManager.FindByEmailAsync(Input.Email);
            if (postojećiKorisnik != null)
            {
                ModelState.AddModelError("Input.Email", "A user with this email address already exists.");
                return Page();
            }

            var user = CreateUser();

            user.Ime = Input.Ime;
            user.Prezime = Input.Prezime;
            user.PhoneNumber = Input.PhoneNumber;
            user.JMBG = Input.JMBG;
            user.DatumRodjenja = birthDateOnly;
            user.EmailConfirmed = true;

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                string defaultRole = "Client";

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
                        Stanje = 0.00,
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

                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

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
        string kodPoslovnice = "000";

        string brojKlijenta = "";
        for (int i = 0; i < 8; i++)
        {
            brojKlijenta += random.Next(0, 10).ToString();
        }
        string kodBanke = "999";

        string baza = kodBanke + kodPoslovnice + brojKlijenta;

        long bazaLong = long.Parse(baza) * 100;
        int ostatak = (int)(bazaLong % 97);
        int kontrolni = 98 - ostatak;

        return baza + kontrolni.ToString("D2");
    }

    private string GenerateValidBosnianIBAN(string accountNumber)
    {
        string broj = accountNumber + "111000";

        System.Numerics.BigInteger bigNum = System.Numerics.BigInteger.Parse(broj);
        int ostatak = (int)(bigNum % 97);
        int controlValue = 98 - ostatak;
        string kk = controlValue.ToString("D2");
        return "BA" + kk + accountNumber;
    }

    private bool ValidajJmbgIDatum()
    {
        if (string.IsNullOrEmpty(Input.JMBG) || Input.JMBG.Length < 7) return false;

  
        string jmbgDan = Input.JMBG.Substring(0, 2);
        string jmbgMesec = Input.JMBG.Substring(2, 2);
        string jmbgGodina = Input.JMBG.Substring(4, 3);

        string datumDan = Input.DatumRodjenja.ToString("dd");
        string datumMesec = Input.DatumRodjenja.ToString("MM");
        string datumGodina = Input.DatumRodjenja.ToString("yyyy").Substring(1, 3);

        return jmbgDan == datumDan && jmbgMesec == datumMesec && jmbgGodina == datumGodina;
    }
}