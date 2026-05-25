using System.ComponentModel.DataAnnotations;

namespace arac_kiralama.Models;

/// <summary>
/// Müşteri kayıt (Register) formu: hem giriş bilgisi hem müşteri bilgisi tek formda.
/// Kayıt olunca: Musteri + Kullanici tablolarına yazılır.
/// </summary>
public class MusteriKayitViewModel
{
    // --- Giriş bilgileri (Kullanicilar tablosu) ---
    [Display(Name = "Kullanıcı adı")]
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-50 karakter olmalıdır.")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [Display(Name = "Şifre")]
    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    public string Sifre { get; set; } = string.Empty;

    [Display(Name = "Şifre tekrar")]
    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [Compare(nameof(Sifre), ErrorMessage = "Şifreler eşleşmiyor.")]  // İki şifre alanı aynı mı?
    [DataType(DataType.Password)]
    public string SifreTekrar { get; set; } = string.Empty;

    // --- Müşteri bilgileri (Musteriler tablosu) ---
    [Display(Name = "Ad soyad")]
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Ad soyad 2-120 karakter olmalıdır.")]
    public string AdSoyad { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    [Required(ErrorMessage = "Telefon zorunludur.")]
    [StringLength(30, MinimumLength = 7, ErrorMessage = "Telefon 7-30 karakter olmalıdır.")]
    public string Telefon { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [StringLength(200)]
    public string? Email { get; set; }

    [Display(Name = "TC Kimlik No")]
    [StringLength(11)]
    [RegularExpression(@"^$|^\d{11}$", ErrorMessage = "TC kimlik 11 rakam olmalı veya boş bırakılmalıdır.")]
    public string? TcKimlikNo { get; set; }
}
