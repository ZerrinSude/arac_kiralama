using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace arac_kiralama.Models;

/// <summary>
/// Giriş hesapları tablosu. Admin veya Müşteri rolü ile sisteme girilir.
/// Müşteri hesabı MusteriId ile Müşteriler tablosuna bağlanır.
/// </summary>
public class Kullanici
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string KullaniciAdi { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Sifre { get; set; } = string.Empty;  // Demo projede düz metin; gerçek projede hash kullanılır

    [Required]
    [StringLength(20)]
    public string Rol { get; set; } = string.Empty;    // "Admin" veya "Musteri" (Roller sınıfındaki sabitler)

    public int? MusteriId { get; set; }                // Admin için null, müşteri için dolu

    [ForeignKey(nameof(MusteriId))]
    public Musteri? Musteri { get; set; }              // Navigation property: ilişkili müşteri kaydı
}

/// <summary>Rol adları sabit — yazım hatası önlenir.</summary>
public static class Roller
{
    public const string Admin = "Admin";
    public const string Musteri = "Musteri";
}
