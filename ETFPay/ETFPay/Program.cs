using ETFPay.Data;
using ETFPay.Models;
using ETFPay.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<Osoba>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<KursService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Klijent", "Uposlenik", "Admin" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }


    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Osoba>>();

    string adminEmail = "admin@etfpay.com";
    string adminPassword = "Admin-123";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var user = new Osoba
        {
            Ime = "Omer",
            Prezime = "Meslasa",
            JMBG = "23687527372",
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, adminPassword);

        if (result.Succeeded)
        {
            Console.WriteLine("Korisnik uspješno kreiran.");

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.ChangeTracker.Clear(); 
            var adminNovi = await userManager.FindByEmailAsync(adminEmail);

            if (adminNovi != null)
            {
                var rez = await userManager.AddToRoleAsync(adminNovi, "Admin");

                if (rez.Succeeded)
                {
                    Console.WriteLine("Uloga uspješno dodijeljena adminu");
                }
                else
                {
                    Console.WriteLine("Greška pri dodjeli uloge adminu");
                }
            }
        }
        else
        {
            Console.WriteLine("Kreiranje admina nije uspjelo:");
        }
    }

}



if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();