using arac_kiralama.Data;
using arac_kiralama.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace arac_kiralama.Controllers; // Müşteri CRUD işlemleri için controller

/// <summary>
/// CONTROLLER: Müşteri CRUD — sadece Admin erişebilir.
/// </summary>
[Authorize(Roles = Roller.Admin)] // Sadece Admin rolündeki kullanıcılar erişebilir
public class MusterilerController : Controller // Müşteri CRUD işlemleri için controller
{
    private readonly AppDbContext _db;

    public MusterilerController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() => // Müşterileri ada göre sıralayarak listele
        View(await _db.Musteriler.OrderBy(m => m.AdSoyad).ToListAsync()); // Veritabanından tüm müşterileri çek ve ada göre sırala

    public async Task<IActionResult> Details(int? id) // Müşteri detaylarını göster
    {
        if (id == null) return NotFound(); // ID sağlanmazsa 404 döndür
        var musteri = await _db.Musteriler.FirstOrDefaultAsync(m => m.Id == id); /// Veritabanından ID'ye göre müşteriyi bul
        if (musteri == null) return NotFound();
        return View(musteri); // Müşteri bulunduysa detay sayfasını göster
    }

    public IActionResult Create() => View(); // Müşteri oluşturma formunu göster (GET)

    [HttpPost]
    [ValidateAntiForgeryToken] 
    public async Task<IActionResult> Create([Bind("AdSoyad,Telefon,Email,TcKimlikNo")] Musteri musteri)// Müşteri oluşturma işlemi (POST)
    {
        if (!ModelState.IsValid) return View(musteri); // Model doğrulaması başarısızsa formu tekrar göster
        _db.Add(musteri); // Yeni müşteriyi veritabanına ekle
        await _db.SaveChangesAsync(); // Değişiklikleri kaydet (INSERT INTO)
        return RedirectToAction(nameof(Index));// Başarılıysa müşteri listesine yönlendir
    }

    public async Task<IActionResult> Edit(int? id) // Müşteri düzenleme formunu göster (GET)
    {
        if (id == null) return NotFound(); 
        var musteri = await _db.Musteriler.FindAsync(id);
        if (musteri == null) return NotFound();
        return View(musteri); // Müşteri bulunduysa düzenleme formunu göster
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,AdSoyad,Telefon,Email,TcKimlikNo")] Musteri musteri) // Müşteri düzenleme işlemi (POST)
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

    // GET: Musteriler/Delete/5
    // Kullanıcı listeden "Sil" butonuna ilk bastığında burası çalışır
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        // 1. KORUMA (GET AŞAMASI): Kullanıcı daha onay ekranını görmeden önce kontrol et
        bool hasRentals = await _db.Kiralamalar.AnyAsync(k => k.MusteriId == id);
        if (hasRentals)
        {
            // Eğer kiralama varsa onay ekranına hiç gitme, direkt listeye at ve mesajı ver!
            TempData["HataMesaji"] = "Bu müşteriye ait aktif veya geçmiş kiralama kaydı bulunduğundan silme işlemi gerçekleştirilemez!";
            return RedirectToAction(nameof(Index));
        }

        var musteri = await _db.Musteriler.FirstOrDefaultAsync(m => m.Id == id);
        if (musteri == null) return NotFound();

        return View(musteri);
    }

    // POST: Musteriler/Delete/5
    // Kullanıcı onay ekranında kırmızı "Sil" butonuna bastığında burası çalışır
    // POST: Musteriler/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // 1. AŞAMA: Ön kontrol (LINQ Koruması)
        bool hasRentals = await _db.Kiralamalar.AnyAsync(k => k.MusteriId == id);
        if (hasRentals)
        {
            TempData["HataMesaji"] = "Bu müşteriye ait aktif veya geçmiş kiralama kaydı bulunduğundan silme işlemi gerçekleştirilemez!";
            return RedirectToAction(nameof(Index));
        }

        // 2. AŞAMA: try-catch Zırhı (Veritabanından gelebilecek SQLite Error 19'u havada yakalar)
        try
        {
            var musteri = await _db.Musteriler.FindAsync(id);
            if (musteri != null)
            {
                _db.Musteriler.Remove(musteri);
                await _db.SaveChangesAsync(); // Hatanın patladığı yer burasıydı, artık try içinde!
            }
        }
        catch (Exception)
        {
            // Eğer üstteki LINQ kontrolünden kaçan gizli bir ilişkisel bağ varsa (Örn: Kullanıcılar tablosu bağı)
            // Veritabanı hata fırlattığı an burası çalışır, sistem çökmez!
            TempData["HataMesaji"] = "Veritabanı Kısıt Engeli: Bu müşteri sistemi üzerinde ilişkili başka verilere (Kullanıcı hesabı veya kiralama faturası) sahip olduğundan silinemez!";
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index));
    }
    private Task<bool> MusteriExists(int id) => _db.Musteriler.AnyAsync(e => e.Id == id);
}
