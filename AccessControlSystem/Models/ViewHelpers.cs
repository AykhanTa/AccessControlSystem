using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace AccessControlSystem.Models;

/// <summary>Razor view-larda təkrarlanan kiçik HTML fraqmentləri (avatar, status chip).</summary>
public static class ViewHelpers
{
    private const string PersonIcon =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2\"/><circle cx=\"12\" cy=\"7\" r=\"4\"/></svg>";

    private static string Enc(string? s) => HtmlEncoder.Default.Encode(s ?? string.Empty);

    /// <summary>Qonaq avatarı — şəkil varsa foto, yoxdursa placeholder ikon.</summary>
    public static IHtmlContent Avatar(string? photo, string? name)
    {
        if (!string.IsNullOrWhiteSpace(photo) &&
            (photo.Contains('/') || photo.Contains('.')) &&
            System.Text.RegularExpressions.Regex.IsMatch(photo, @"\.(jpe?g|png|webp|gif|svg)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return new HtmlString(
                $"<div class=\"g-avatar\"><img src=\"{Enc(photo)}\" alt=\"{Enc(name)}\" loading=\"lazy\" /></div>");
        }
        return new HtmlString($"<div class=\"g-avatar empty\">{PersonIcon}</div>");
    }

    /// <summary>Status "chip" — in/out/late.</summary>
    public static IHtmlContent StatusChip(string status)
    {
        var (label, cls) = status switch
        {
            "in" => ("Binadadır", "in"),
            "late" => ("Gecikib", "late"),
            _ => ("Çıxıb", "out"),
        };
        return new HtmlString($"<span class=\"chip {cls}\">{label}</span>");
    }
}
