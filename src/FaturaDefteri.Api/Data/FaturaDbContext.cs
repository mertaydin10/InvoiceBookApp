using FaturaDefteri.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FaturaDefteri.Api.Data;

public class FaturaDbContext(DbContextOptions<FaturaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<IssuerProfile> IssuerProfiles => Set<IssuerProfile>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<IssuerProfile>(e =>
        {
            e.Property(x => x.TradeName).HasMaxLength(160).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(8);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Client>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.HasIndex(x => new { x.UserId, x.Name });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.Property(x => x.Number).HasMaxLength(32).IsRequired();
            e.HasIndex(x => new { x.UserId, x.Number }).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Lines).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceLine>(e =>
        {
            e.Property(x => x.Description).HasMaxLength(240).IsRequired();
            e.Property(x => x.Quantity).HasPrecision(12, 2);
            e.Property(x => x.UnitPrice).HasPrecision(12, 2);
            e.Property(x => x.VatRate).HasPrecision(5, 2);
        });
    }
}
