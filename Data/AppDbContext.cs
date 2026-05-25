using arac_kiralama.Models;
using Microsoft.EntityFrameworkCore;

namespace arac_kiralama.Data;

/// <summary>
/// MODEL katmanı ile VERİTABANI arasındaki köprü (Entity Framework DbContext).
/// Her DbSet = bir tablo. OnModelCreating = tablolar arası ilişki (Foreign Key) kuralları.
/// </summary>
public class AppDbContext : DbContext
{
    // Constructor: Program.cs'deki AddDbContext ile bağlantı ayarı buraya gelir
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // --- TABLOLAR (DbSet = SQL tablosunun C# karşılığı) ---
    public DbSet<Arac> Araclar => Set<Arac>();
    public DbSet<Musteri> Musteriler => Set<Musteri>();
    public DbSet<Kiralama> Kiralamalar => Set<Kiralama>();
    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Kullanıcı adı benzersiz olsun; müşteri silinirse kullanıcı kaydı kalabilir (SetNull)
        modelBuilder.Entity<Kullanici>(entity =>
        {
            entity.HasIndex(k => k.KullaniciAdi).IsUnique();
            entity.HasOne(k => k.Musteri)
                .WithMany()
                .HasForeignKey(k => k.MusteriId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Kiralama -> Araç ve Müşteri (çoka-bir ilişki)
        // Restrict: bağlı kiralama varken araç/müşteri silinemez
        modelBuilder.Entity<Kiralama>(entity =>
        {
            entity.HasOne(k => k.Arac)
                .WithMany(a => a.Kiralamalar)
                .HasForeignKey(k => k.AracId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(k => k.Musteri)
                .WithMany(m => m.Kiralamalar)
                .HasForeignKey(k => k.MusteriId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
