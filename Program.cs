// =============================================================================
// Program.cs — Uygulamanın giriş noktası (başlangıç dosyası)
// Burada: veritabanı, giriş (cookie), MVC ve HTTP pipeline yapılandırılır.
// =============================================================================

using arac_kiralama.Data;
using arac_kiralama.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

// Web uygulaması oluşturucu (builder pattern)
var builder = WebApplication.CreateBuilder(args);

// --- SERVİS KAYITLARI (Dependency Injection) ---
// Controller'lara AppDbContext otomatik enjekte edilir.

// Entity Framework + SQLite bağlantısı (appsettings.json içindeki ConnectionStrings)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cookie tabanlı kimlik doğrulama (oturum çerezi)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/LoginMusteri";      // Giriş yapmayan kullanıcı buraya yönlendirilir
        options.AccessDeniedPath = "/Home/Index";         // Yetkisiz erişimde ana sayfa
        options.ExpireTimeSpan = TimeSpan.FromHours(8);   // Oturum süresi
    });

builder.Services.AddAuthorization();           // [Authorize] öznitelikleri için
builder.Services.AddControllersWithViews();    // MVC Controller + Razor View desteği

var app = builder.Build();

// --- UYGULAMA AÇILIRKEN: veritabanı ve demo veriler ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();                              // Tablolar yoksa oluştur
    await SeedData.EnsureKullanicilarTableAsync(db);          // Eski DB'ye Kullanicilar tablosu ekle
    await SeedData.SeedAsync(db);                             // Demo araç / müşteri / kullanıcı
}

// --- HTTP İŞLEM SIRASI (sıra önemli!) ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();   // HTTP -> HTTPS
app.UseStaticFiles();        // wwwroot (css, js, resim)
app.UseRouting();            // URL yönlendirme
app.UseAuthentication();     // Kim giriş yapmış? (önce auth)
app.UseAuthorization();      // Yetkisi var mı? (sonra authorize)

// Varsayılan rota: /Controller/Action/Id  örn: /Araclar/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Demo veri ve Kullanicilar tablosu yardımcı sınıfı
static class SeedData
{
    // Eski veritabanı dosyasında Kullanicilar tablosu yoksa SQL ile ekler
    public static async Task EnsureKullanicilarTableAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS Kullanicilar (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                KullaniciAdi TEXT NOT NULL,
                Sifre TEXT NOT NULL,
                Rol TEXT NOT NULL,
                MusteriId INTEGER NULL,
                FOREIGN KEY (MusteriId) REFERENCES Musteriler(Id)
            );
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Kullanicilar_KullaniciAdi ON Kullanicilar (KullaniciAdi);
            """);
    }

    // İlk çalıştırmada örnek araç, müşteri ve giriş hesapları
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!db.Araclar.Any())
        {
            db.Araclar.AddRange(
                new Arac { Marka = "Toyota", Model = "Corolla", Plaka = "34 ABC 123", GunlukUcret = 1200, Musait = true },
                new Arac { Marka = "Renault", Model = "Clio", Plaka = "06 XYZ 99", GunlukUcret = 850, Musait = true });
        }

        Musteri? demoMusteri = null;
        if (!db.Musteriler.Any())
        {
            demoMusteri = new Musteri
            {
                AdSoyad = "Demo Müşteri",
                Telefon = "05551234567",
                Email = "demo@ornek.com"
            };
            db.Musteriler.Add(demoMusteri);
            await db.SaveChangesAsync();
        }
        else
        {
            demoMusteri = await db.Musteriler.FirstAsync();
        }

        if (!await db.Kullanicilar.AnyAsync())
        {
            db.Kullanicilar.AddRange(
                new Kullanici { KullaniciAdi = "admin", Sifre = "admin123", Rol = Roller.Admin },
                new Kullanici { KullaniciAdi = "musteri", Sifre = "musteri123", Rol = Roller.Musteri, MusteriId = demoMusteri.Id });
            await db.SaveChangesAsync();
        }
        else if (!await db.Kullanicilar.AnyAsync(k => k.Rol == Roller.Admin))
        {
            db.Kullanicilar.Add(new Kullanici { KullaniciAdi = "admin", Sifre = "admin123", Rol = Roller.Admin });
            await db.SaveChangesAsync();
        }
    }
}
