using ETFPay.Data;
using ETFPay.Filters;
using ETFPay.Models;
using ETFPay.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<Osoba>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ClientAccountViewBagFilter>();
});
builder.Services.AddScoped<ClientAccountViewBagFilter>();
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<KursService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Client", "Uposlenik", "Admin", "Zastitar", "Direktor", "Domar", "Blagajnik" };

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

var blockedEmailConfirmationPaths = new[]
{
    "/identity/account/confirmemail",
    "/identity/account/confirmemailchange",
    "/identity/account/resendemailconfirmation",
    "/identity/account/registerconfirmation"
};

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
    if (blockedEmailConfirmationPaths.Any(p => path.StartsWith(p, StringComparison.Ordinal)))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
