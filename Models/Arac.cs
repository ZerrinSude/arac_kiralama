using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace arac_kiralama.Models;

/// <summary>
/// MODEL: Araçlar tablosunun C# karşılığı (Entity).
/// Bir aracın birden fazla kiralama kaydı olabilir (1-N ilişki).
/// </summary>
public class Arac
{
    public int Id { get; set; }  // Birincil anahtar (Primary Key), otomatik artar

    [Display(Name = "Marka")]   // View'da etiket metni
    [Required(ErrorMessage = "Marka zorunludur.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Marka 2-60 karakter olmalıdır.")]
    public string Marka { get; set; } = string.Empty;

    [Display(Name = "Model")]
    [Required(ErrorMessage = "Model zorunludur.")]
    [StringLength(60, MinimumLength = 1, ErrorMessage = "Model en fazla 60 karakter olabilir.")]
    public string Model { get; set; } = string.Empty;

    [Display(Name = "Plaka")]
    [Required(ErrorMessage = "Plaka zorunludur.")]
    [StringLength(15, ErrorMessage = "Plaka en fazla 15 karakter olabilir.")]
    public string Plaka { get; set; } = string.Empty;

    [Display(Name = "Günlük ücret (₺)")]
    // Sayısal Range kullanıyoruz (string "0.01" Türkçe Windows'ta hata verebilir)
    [Range(0.01, 999999, ErrorMessage = "Günlük ücret 0'dan büyük olmalıdır.")]
    public decimal GunlukUcret { get; set; }

    [Display(Name = "Müsait")]
    public bool Musait { get; set; } = true;  // Kiralandığında false yapılır

    // Navigation: bu araca ait kiralamalar (EF ilişki)
    public ICollection<Kiralama> Kiralamalar { get; set; } = new List<Kiralama>();

    [NotMapped]  // Veritabanına yazılmaz, sadece dropdown/liste metni için
    [Display(Name = "Araç")]
    public string ListeEtiketi => $"{Marka} {Model} ({Plaka})";
}
