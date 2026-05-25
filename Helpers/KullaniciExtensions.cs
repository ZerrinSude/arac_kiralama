using System.Security.Claims;
using arac_kiralama.Models;

namespace arac_kiralama.Helpers;

/// <summary>
/// Giriş yapmış kullanıcı (ClaimsPrincipal) için yardımcı metotlar.
/// View ve Controller'da User.IsAdmin() gibi kısa kullanım sağlar.
/// </summary>
public static class KullaniciExtensions
{
    // Kullanıcı Admin rolünde mi?
    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole(Roller.Admin);

    // Kullanıcı Müşteri rolünde mi?
    public static bool IsMusteri(this ClaimsPrincipal user) =>
        user.IsInRole(Roller.Musteri);

    // Girişte claim olarak eklenen MusteriId (müşteri sadece kendi kiralamalarını görür)
    public static int? GetMusteriId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("MusteriId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
