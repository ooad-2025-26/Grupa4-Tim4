using ETFPay.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ETFPay.Data
{
    public class ApplicationDbContext : IdentityDbContext<Osoba>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Racun> Racun { get; set; }
        public DbSet<Transakcija> Transakcija { get; set; }
        public DbSet<Predlozak> Predlozak { get; set; }
        public DbSet<Kurs> Kurs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Racun>().ToTable("Racun");
            builder.Entity<Transakcija>().ToTable("Transakcija");
            builder.Entity<Predlozak>().ToTable("Predlozak");
            builder.Entity<Kurs>().ToTable("Kurs");
        }
    }
}