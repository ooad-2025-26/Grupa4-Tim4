using ETFPay.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ETFPay.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        
        public DbSet<Osoba> Osoba { get; set; }
        public DbSet<Racun> Racun { get; set; }
        public DbSet<Transakcija> Transakcija { get; set; }
        public DbSet<Predlozak> Predlozak { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Osoba>().ToTable("Osoba");
            builder.Entity<Racun>().ToTable("Racun");
            builder.Entity<Transakcija>().ToTable("Transakcija");
            builder.Entity<Predlozak>().ToTable("Predlozak");

            base.OnModelCreating(builder);
        }
        public DbSet<ETFPay.Models.Kurs> Kurs { get; set; } = default!;
    }
}
