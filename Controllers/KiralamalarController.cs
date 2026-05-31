using arac_kiralama.Data;
using arac_kiralama.Helpers;
using arac_kiralama.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace arac_kiralama.Controllers;

/// <summary>
/// CONTROLLER: Kiralama işlemleri.
/// Müşteri: sadece kendi kayıtları + kiralama talebi (toplam tutar sistem hesaplar).
/// Admin: tüm kiralamalar, düzenleme ve silme.
/// </summary>
[Authorize]
public class KiralamalarController : Controller
{
    private readonly AppDbContext _db;

    public KiralamalarController(AppDbContext db) => _db = db;

    // READ — Include ile ilişkili Araç ve Müşteri birlikte çekilir (JOIN)
    public async Task<IActionResult> Index()
    {
        var query = _db.Kiralamalar.Include(k => k.Arac).Include(k => k.Musteri).AsQueryable();

        if (User.IsMusteri())
        {
            var musteriId = User.GetMusteriId();
            if (!musteriId.HasValue) return Forbid();
            query = query.Where(k => k.MusteriId == musteriId.Value);
        }

        var liste = await query.OrderByDescending(k => k.BaslangicTarihi).ToListAsync();
        return View(liste);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var kiralama = await GetKiralamaAsync(id.Value);
        if (kiralama == null) return NotFound();
        return View(kiralama);
    }

    private async Task<Kiralama?> GetKiralamaAsync(int id)
    {
        var kiralama = await _db.Kiralamalar.Include(k => k.Arac).Include(k => k.Musteri)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (kiralama == null) return null;
        if (User.IsMusteri() && kiralama.MusteriId != User.GetMusteriId())
            return null;
        return kiralama;
    }

    /// <summary>Dropdown listelerini ViewBag ile View'a taşır.</summary>
    private async Task DoldurListelerAsync(Kiralama? secili = null, bool musteriSabit = false)
    {
        var aracQuery = _db.Araclar.AsQueryable();
        if (User.IsMusteri() || secili == null)
            aracQuery = aracQuery.Where(a => a.Musait);
        var araclar = await aracQuery.OrderBy(a => a.Marka).ToListAsync();

        var aracItems = new List<SelectListItem>
        {
            new() { Value = "", Text = "-- Araç seçin --", Selected = secili is null || secili.AracId == 0 }
        };
        aracItems.AddRange(araclar.Select(a => new SelectListItem
        {
            Value = a.Id.ToString(),
            Text = a.ListeEtiketi,
            Selected = secili?.AracId == a.Id
        }));
        ViewBag.AracId = aracItems;
        ViewBag.AracVar = araclar.Count > 0;

        if (musteriSabit && User.IsMusteri())
        {
            var mid = User.GetMusteriId();
            ViewBag.MusteriSabit = true;
            ViewBag.MusteriAdi = await _db.Musteriler.Where(m => m.Id == mid)
                .Select(m => m.AdSoyad).FirstOrDefaultAsync() ?? "Müşteri";
            if (secili != null && mid.HasValue)
                secili.MusteriId = mid.Value;
        }
        else if (User.IsInRole(Roller.Admin))
        {
            var musteriler = await _db.Musteriler.OrderBy(m => m.AdSoyad).ToListAsync();
            var musteriItems = new List<SelectListItem>
            {
                new() { Value = "", Text = "-- Müşteri seçin --", Selected = secili is null || secili.MusteriId == 0 }
            };
            musteriItems.AddRange(musteriler.Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = m.ListeEtiketi,
                Selected = secili?.MusteriId == m.Id
            }));
            ViewBag.MusteriId = musteriItems;
            ViewBag.MusteriVar = musteriler.Count > 0;
        }
    }

    /// <summary>
    /// Toplam tutar = günlük ücret × gün sayısı. Müşteri bu alanı girmez.
    /// </summary>
    private async Task<bool> HesaplaToplamTutarAsync(Kiralama kiralama)
    {
        if (kiralama.AracId <= 0)
        {
            ModelState.AddModelError(nameof(Kiralama.AracId), "Araç seçilmelidir.");
            return false;
        }

        var arac = await _db.Araclar.AsNoTracking().FirstOrDefaultAsync(a => a.Id == kiralama.AracId);
        if (arac == null)
        {
            ModelState.AddModelError(nameof(Kiralama.AracId), "Seçilen araç bulunamadı.");
            return false;
        }

        var gunSayisi = (kiralama.BitisTarihi.Date - kiralama.BaslangicTarihi.Date).Days;
        if (gunSayisi < 1)
        {
            ModelState.AddModelError(nameof(Kiralama.BitisTarihi),
                "Bitiş tarihi, başlangıçtan en az bir gün sonra olmalıdır.");
            return false;
        }

        kiralama.ToplamTutar = gunSayisi * arac.GunlukUcret; //hesaplamayı otomatik olarak burada hesaplatıyoruz
        ViewBag.HesaplananGun = gunSayisi;
        ViewBag.GunlukUcret = arac.GunlukUcret;
        return true;
    }

    public async Task<IActionResult> Create()
    {
        var model = new Kiralama();
        if (User.IsMusteri())
        {
            var mid = User.GetMusteriId();
            if (!mid.HasValue) return Forbid();
            model.MusteriId = mid.Value;
        }

        await DoldurListelerAsync(model, musteriSabit: User.IsMusteri());

        if (ViewBag.AracVar == false)
        {
            TempData["Hata"] = "Şu anda müsait araç bulunmuyor.";
            return RedirectToAction(nameof(Index));
        }
        if (User.IsInRole(Roller.Admin) && ViewBag.MusteriVar == false)
        {
            TempData["Hata"] = "Kiralama için önce müşteri kaydı ekleyin.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // ToplamTutar Bind listesinde YOK — kullanıcıdan alınmaz, HesaplaToplamTutarAsync ile doldurulur
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AracId,MusteriId,BaslangicTarihi,BitisTarihi")] Kiralama kiralama)
    {
        if (User.IsMusteri())
        {
            var mid = User.GetMusteriId();
            if (!mid.HasValue) return Forbid();
            kiralama.MusteriId = mid.Value;
        }

        await DoldurListelerAsync(kiralama, musteriSabit: User.IsMusteri());
        if (!ModelState.IsValid) return View(kiralama);
        if (!await HesaplaToplamTutarAsync(kiralama)) return View(kiralama);

        try
        {
            _db.Add(kiralama);
            await _db.SaveChangesAsync();
            var arac = await _db.Araclar.FindAsync(kiralama.AracId);
            if (arac != null) { arac.Musait = false; await _db.SaveChangesAsync(); }
            TempData["Basari"] = "Kiralama kaydı eklendi.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Kayıt oluşturulamadı.");
            return View(kiralama);
        }
    }

    [Authorize(Roles = Roller.Admin)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var kiralama = await _db.Kiralamalar.FindAsync(id);
        if (kiralama == null) return NotFound();
        await DoldurListelerAsync(kiralama);
        return View(kiralama);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roller.Admin)]
    public async Task<IActionResult> Edit(int id, [Bind("Id,AracId,MusteriId,BaslangicTarihi,BitisTarihi")] Kiralama kiralama)
    {
        if (id != kiralama.Id) return NotFound();
        await DoldurListelerAsync(kiralama);
        if (!ModelState.IsValid) return View(kiralama);
        if (!await HesaplaToplamTutarAsync(kiralama)) return View(kiralama);
        try
        {
            _db.Update(kiralama);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await KiralamaExists(kiralama.Id)) return NotFound();
            throw;
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roller.Admin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var kiralama = await GetKiralamaAsync(id.Value);
        if (kiralama == null) return NotFound();
        return View(kiralama);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roller.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var kiralama = await _db.Kiralamalar.FindAsync(id);
        if (kiralama != null)
        {
            var aracId = kiralama.AracId;
            _db.Kiralamalar.Remove(kiralama);
            await _db.SaveChangesAsync();
            if (!await _db.Kiralamalar.AnyAsync(k => k.AracId == aracId))
            {
                var arac = await _db.Araclar.FindAsync(aracId);
                if (arac != null) { arac.Musait = true; await _db.SaveChangesAsync(); }
            }
        }
        return RedirectToAction(nameof(Index));
    }

    private Task<bool> KiralamaExists(int id) => _db.Kiralamalar.AnyAsync(e => e.Id == id);
}
