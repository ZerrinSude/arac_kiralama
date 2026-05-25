using arac_kiralama.Data;
using arac_kiralama.Helpers;
using arac_kiralama.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace arac_kiralama.Controllers;

/// <summary>
/// CONTROLLER: Ana sayfa. Giriş yapılmamış / Admin / Müşteri için farklı özet gösterir.
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _db;  // Veritabanı erişimi (DI ile gelir)

    public HomeController(ILogger<HomeController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    // GET /Home/Index — Herkes görebilir (giriş zorunlu değil)
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        // View'a taşınacak model (strongly-typed View)
        var vm = new HomeIndexViewModel
        {
            KullaniciAdi = User.Identity?.Name,
            AdminMi = User.IsAdmin(),
            MusteriMi = User.IsMusteri()
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            // LINQ: veritabanından sayıları oku
            vm.AracSayisi = await _db.Araclar.CountAsync();
            vm.MusaitAracSayisi = await _db.Araclar.CountAsync(a => a.Musait);

            if (User.IsAdmin())
            {
                vm.MusteriSayisi = await _db.Musteriler.CountAsync();
                vm.KiralamaSayisi = await _db.Kiralamalar.CountAsync();
            }
            else if (User.IsMusteri())
            {
                var musteriId = User.GetMusteriId();
                if (musteriId.HasValue)
                    vm.BenimKiralamaSayim = await _db.Kiralamalar.CountAsync(k => k.MusteriId == musteriId.Value);
            }
        }

        return View(vm);  // Views/Home/Index.cshtml + vm modeli
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
