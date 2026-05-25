using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace arac_kiralama.Models;

/// <summary>
/// MODEL: Kiralamalar tablosu — Araç ve Müşteri arasında ara tablo (ilişki tablosu gibi).
/// ToplamTutar Controller'da otomatik hesaplanır (günlük ücret × gün sayısı).
/// </summary>
public class Kiralama : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Araç")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir araç seçin.")]  // int'te Required 0'ı yakalamaz
    public int AracId { get; set; }

    [Display(Name = "Müşteri")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir müşteri seçin.")]
    public int MusteriId { get; set; }

    [Display(Name = "Başlangıç")]
    [DataType(DataType.Date)]
    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;

    [Display(Name = "Bitiş")]
    [DataType(DataType.Date)]
    public DateTime BitisTarihi { get; set; } = DateTime.Today.AddDays(1);

    [Display(Name = "Toplam tutar (₺)")]
    public decimal ToplamTutar { get; set; }  // Formdan gelmez; sistem hesaplar

    [ForeignKey(nameof(AracId))]
    public Arac? Arac { get; set; }           // JOIN ile araç bilgisi çekmek için

    [ForeignKey(nameof(MusteriId))]
    public Musteri? Musteri { get; set; }

    // Özel doğrulama: bitiş > başlangıç olmalı
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BitisTarihi <= BaslangicTarihi)
        {
            yield return new ValidationResult(
                "Bitiş tarihi, başlangıç tarihinden sonra olmalıdır.",
                new[] { nameof(BitisTarihi) });
        }
    }
}
