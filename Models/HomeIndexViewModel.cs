namespace arac_kiralama.Models;

/// <summary>
/// Ana sayfa (Home/Index) için özet veriler.
/// Controller LINQ ile sayıları hesaplar, View @model ile gösterir.
/// </summary>
public class HomeIndexViewModel
{
    public string? KullaniciAdi { get; set; }
    public bool AdminMi { get; set; }
    public bool MusteriMi { get; set; }

    // Admin paneli kartları
    public int AracSayisi { get; set; }
    public int MusteriSayisi { get; set; }
    public int KiralamaSayisi { get; set; }
    public int MusaitAracSayisi { get; set; }

    // Müşteri paneli: sadece kendi kiralama sayısı
    public int BenimKiralamaSayim { get; set; }
}
