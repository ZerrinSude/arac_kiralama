using arac_kiralama.Data;
using arac_kiralama.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace arac_kiralama.Controllers;

/// <summary>
/// CONTROLLER: Araç CRUD işlemleri.
/// [Authorize]: giriş zorunlu. Create/Edit/Delete sadece Admin.
/// Müşteri sadece müsait araçları listeler.
/// </summary>
[Authorize]
public class AraclarController : Controller
{
    private readonly AppDbContext _db;

    public AraclarController(AppDbContext db) => _db = db;

    // READ — Liste (LINQ)
    public async Task<IActionResult> Index()
    {
        var query = _db.Araclar.AsQueryable();
        if (User.IsInRole(Roller.Musteri))
            query = query.Where(a => a.Musait);  // Müşteri sadece kiralanabilir araçları görür

        var liste = await query.OrderBy(a => a.Marka).ThenBy(a => a.Model).ToListAsync();
        return View(liste);  // Liste modeli View'a gider
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var arac = await _db.Araclar.FirstOrDefaultAsync(m => m.Id == id);
        if (arac == null) return NotFound();
        return View(arac);
    }

    // CREATE — GET: boş form
    [Authorize(Roles = Roller.Admin)]
    public IActionResult Create() => View(new Arac { Musait = true });

    // CREATE — POST: form verisi model binding ile Arac nesnesine bağlanır
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roller.Admin)]
    public async Task<IActionResult> Create([Bind("Marka,Model,Plaka,GunlukUcret,Musait")] Arac arac)
    {
        arac.Plaka = arac.Plaka?.Trim() ?? string.Empty;
        arac.Marka = arac.Marka?.Trim() ?? string.Empty;
        arac.Model = arac.Model?.Trim() ?? string.Empty;

        if (arac.GunlukUcret <= 0)
            ModelState.AddModelError(nameof(Arac.GunlukUcret), "Günlük ücret girilmelidir.");

        if (!ModelState.IsValid)
            return View(arac);  // Hata varsa formu tekrar göster

        if (await _db.Araclar.AnyAsync(a => a.Plaka == arac.Plaka))
        {
            ModelState.AddModelError(nameof(Arac.Plaka), "Bu plaka zaten kayıtlı.");
            return View(arac);
        }

        try
        {
            _db.Add(arac);
            await _db.SaveChangesAsync();  // INSERT INTO Araclar
            TempData["Basari"] = $"{arac.Marka} {arac.Model} araç listesine eklendi.";
            return RedirectToAction(nameof(Index));  // PRG pattern: Post-Redirect-Get
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Kayıt veritabanına yazılamadı.");
            return View(arac);
        }
    }

    [Authorize(Roles = Roller.Admin)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var arac = await _db.Araclar.FindAsync(id);
        if (arac == null) return NotFound();
        return View(arac);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roller.Admin)]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Marka,Model,Plaka,GunlukUcret,Musait")] Arac arac)
    {
        if (id != arac.Id) return NotFound();
        arac.Plaka = arac.Plaka?.Trim() ?? string.Empty;
        arac.Marka = arac.Marka?.Trim() ?? string.Empty;
        arac.Model = arac.Model?.Trim() ?? string.Empty;

        if (arac.GunlukUcret <= 0)
            ModelState.AddModelError(nameof(Arac.GunlukUcret), "Günlük ücret 0'dan büyük olmalıdır.");

        if (!ModelState.IsValid) return View(arac);

        if (await _db.Araclar.AnyAsync(a => a.Plaka == arac.Plaka && a.Id != arac.Id))
        {
            ModelState.AddModelError(nameof(Arac.Plaka), "Bu plaka başka bir araçta kullanılıyor.");
            return View(arac);
        }

        try
        {
            _db.Update(arac);
            await _db.SaveChangesAsync();
            TempData["Basari"] = "Araç bilgileri güncellendi.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await AracExists(arac.Id)) return NotFound();
            throw;
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roller.Admin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var arac = await _db.Araclar.FirstOrDefaultAsync(m => m.Id == id);
        if (arac == null) return NotFound();
        return View(arac);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roller.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var arac = await _db.Araclar.FindAsync(id);
        if (arac != null)
        {
            if (await _db.Kiralamalar.AnyAsync(k => k.AracId == id))
            {
                TempData["Hata"] = "Bu araca ait kiralama kaydı olduğu için silinemez.";
                return RedirectToAction(nameof(Index));
            }
            _db.Araclar.Remove(arac);
            await _db.SaveChangesAsync();
            TempData["Basari"] = "Araç silindi.";
        }
        return RedirectToAction(nameof(Index));
    }

    private Task<bool> AracExists(int id) => _db.Araclar.AnyAsync(e => e.Id == id);
}
