using System.Security.Claims;
using arac_kiralama.Data;
using arac_kiralama.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace arac_kiralama.Controllers;

/// <summary>
/// CONTROLLER: Giriş, çıkış ve müşteri kayıt işlemleri.
/// Cookie authentication ile oturum açılır.
/// </summary>
public class AccountController : Controller
{
    private readonly AppDbContext _db; //Veri tabanına erişimi sağlayan AppDbContext nesnesini sınıfa enjekte ediyorsun

    public AccountController(AppDbContext db) => _db = db;

    // --- GET: Form sayfalarını göster ---
    [AllowAnonymous]
    public IActionResult LoginAdmin() => View(new LoginViewModel()); //Admin giriş, müşteri giriş ve kayıt sayfalarını (GET isteklerini) ekrana basar.

    [AllowAnonymous]
    public IActionResult LoginMusteri() => View(new LoginViewModel());

    [AllowAnonymous]
    public IActionResult Register() => View(new MusteriKayitViewModel());

    // --- POST: Müşteri kayıt (Musteri + Kullanici tablolarına yazar) ---
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]  // CSRF koruması — formda gizli token olmalı
    public async Task<IActionResult> Register(MusteriKayitViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var kullaniciAdi = model.KullaniciAdi.Trim();
        if (await _db.Kullanicilar.AnyAsync(k => k.KullaniciAdi == kullaniciAdi))
        {
            ModelState.AddModelError(nameof(MusteriKayitViewModel.KullaniciAdi), "Bu kullanıcı adı zaten alınmış.");
            return View(model);
        }

        var musteri = new Musteri
        {
            AdSoyad = model.AdSoyad.Trim(),
            Telefon = model.Telefon.Trim(),
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
            TcKimlikNo = string.IsNullOrWhiteSpace(model.TcKimlikNo) ? null : model.TcKimlikNo.Trim()
        };

        // Kullanici.Musteri navigation ile tek SaveChanges'te ikisi de eklenir
        var kullanici = new Kullanici
        {
            KullaniciAdi = kullaniciAdi,
            Sifre = model.Sifre,
            Rol = Roller.Musteri,
            Musteri = musteri
        };

        try
        {
            _db.Kullanicilar.Add(kullanici);
            await _db.SaveChangesAsync();
            await GirisYapAsync(kullanici);  // Kayıt sonrası otomatik giriş
            TempData["Basari"] = "Kayıt başarılı. Hoş geldiniz!";
            return RedirectToAction("Index", "Home");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Kayıt oluşturulamadı. Bilgileri kontrol edip tekrar deneyin.");
            return View(model);
        }
    }

    // --- POST: Admin girişi ---
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginAdmin(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var kullanici = await _db.Kullanicilar
            .FirstOrDefaultAsync(k => k.KullaniciAdi == model.KullaniciAdi && k.Rol == Roller.Admin);

        if (kullanici == null || kullanici.Sifre != model.Sifre)
        {
            ModelState.AddModelError(string.Empty, "Admin kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        await GirisYapAsync(kullanici);
        return RedirectToAction("Index", "Home");
    }

    // --- POST: Müşteri girişi ---
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginMusteri(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var kullanici = await _db.Kullanicilar
            .Include(k => k.Musteri)
            .FirstOrDefaultAsync(k => k.KullaniciAdi == model.KullaniciAdi && k.Rol == Roller.Musteri);

        if (kullanici == null || kullanici.Sifre != model.Sifre)
        {
            ModelState.AddModelError(string.Empty, "Müşteri kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        if (kullanici.MusteriId == null)
        {
            ModelState.AddModelError(string.Empty, "Bu hesap bir müşteri kaydına bağlı değil.");
            return View(model);
        }

        await GirisYapAsync(kullanici);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Oturum aç: Claim'ler (kimlik bilgisi) cookie'ye yazılır.
  /// </summary>
    private async Task GirisYapAsync(Kullanici kullanici)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, kullanici.KullaniciAdi),
            new(ClaimTypes.Role, kullanici.Rol)
        };

        if (kullanici.MusteriId.HasValue)
            claims.Add(new Claim("MusteriId", kullanici.MusteriId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }
}
