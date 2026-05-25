using arac_kiralama.Data;
using arac_kiralama.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace arac_kiralama.Controllers;

/// <summary>
/// CONTROLLER: Müşteri CRUD — sadece Admin erişebilir.
/// </summary>
[Authorize(Roles = Roller.Admin)]
public class MusterilerController : Controller
{
    private readonly AppDbContext _db;

    public MusterilerController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Musteriler.OrderBy(m => m.AdSoyad).ToListAsync());

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var musteri = await _db.Musteriler.FirstOrDefaultAsync(m => m.Id == id);
        if (musteri == null) return NotFound();
        return View(musteri);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AdSoyad,Telefon,Email,TcKimlikNo")] Musteri musteri)
    {
        if (!ModelState.IsValid) return View(musteri);
        _db.Add(musteri);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var musteri = await _db.Musteriler.FindAsync(id);
        if (musteri == null) return NotFound();
        return View(musteri);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,AdSoyad,Telefon,Email,TcKimlikNo")] Musteri musteri)
    {
        if (id != musteri.Id) return NotFound();
        if (!ModelState.IsValid) return View(musteri);
        try
        {
            _db.Update(musteri);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await MusteriExists(musteri.Id)) return NotFound();
            throw;
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var musteri = await _db.Musteriler.FirstOrDefaultAsync(m => m.Id == id);
        if (musteri == null) return NotFound();
        return View(musteri);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var musteri = await _db.Musteriler.FindAsync(id);
        if (musteri != null)
        {
            if (await _db.Kiralamalar.AnyAsync(k => k.MusteriId == id))
            {
                TempData["Hata"] = "Bu müşteriye ait kiralama kaydı olduğu için silinemez.";
                return RedirectToAction(nameof(Index));
            }
            _db.Musteriler.Remove(musteri);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private Task<bool> MusteriExists(int id) => _db.Musteriler.AnyAsync(e => e.Id == id);
}
