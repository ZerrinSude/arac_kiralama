using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace arac_kiralama.Models;

/// <summary>
/// MODEL: Müşteriler tablosu. Kiralama kayıtları MusteriId ile buraya bağlanır.
/// </summary>
public class Musteri
{
    public int Id { get; set; }

    [Display(Name = "Ad soyad")]
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Ad soyad 2-120 karakter olmalıdır.")]
    public string AdSoyad { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    [Required(ErrorMessage = "Telefon zorunludur.")]
    [StringLength(30, MinimumLength = 7, ErrorMessage = "Telefon 7-30 karakter olmalıdır.")]
    public string Telefon { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [DisplayFormat(ConvertEmptyStringToNull = true)]  // Boş string -> null
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [StringLength(200)]
    public string? Email { get; set; }

    [Display(Name = "TC Kimlik No")]
    [DisplayFormat(ConvertEmptyStringToNull = true)]
    [StringLength(11)]
    [RegularExpression(@"^$|^\d{11}$", ErrorMessage = "TC kimlik 11 rakam olmalı veya boş bırakılmalıdır.")]
    public string? TcKimlikNo { get; set; }

    public ICollection<Kiralama> Kiralamalar { get; set; } = new List<Kiralama>();

    [NotMapped]
    public string ListeEtiketi => string.IsNullOrEmpty(Email) ? AdSoyad : $"{AdSoyad} ({Email})";
}
