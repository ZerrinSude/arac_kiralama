using System.ComponentModel.DataAnnotations;

namespace arac_kiralama.Models;

/// <summary>
/// Giriş formları için ViewModel (veritabanı tablosu DEĞİL).
/// Sadece ekrandan Controller'a veri taşımak için kullanılır.
/// </summary>
public class LoginViewModel
{
    [Display(Name = "Kullanıcı adı")]
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [Display(Name = "Şifre")]
    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]  // View'da type="password" üretir
    public string Sifre { get; set; } = string.Empty;
}
