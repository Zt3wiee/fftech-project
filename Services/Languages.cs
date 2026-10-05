using System.Globalization;

namespace Otech.Services
{
    /// <summary>
    /// A language the site is available in. English lives at the site root ("/services");
    /// every other language gets its own URL prefix ("/km/services").
    /// </summary>
    /// <param name="Code">Short code, also the URL prefix and the folder name under /Content.</param>
    /// <param name="Culture">.NET culture used for dates and numbers.</param>
    /// <param name="HtmlLang">Value for &lt;html lang&gt; and hreflang tags.</param>
    /// <param name="NativeName">Name shown in the language switcher, in the language itself.</param>
    /// <param name="ShortName">Compact label for the header button on narrower screens.</param>
    /// <param name="DateFormat">Format for article dates.</param>
    public record SiteLanguage(string Code, string Culture, string HtmlLang, string NativeName, string ShortName, string DateFormat)
    {
        public bool IsDefault => Code == Languages.English.Code;

        /// <summary>"" for English, "/km" for Khmer, and so on.</summary>
        public string UrlPrefix => IsDefault ? "" : "/" + Code;
    }

    public static class Languages
    {
        public const string CookieName = "site_lang";

        public static readonly SiteLanguage English = new("en", "en-US", "en", "English", "EN", "d MMMM yyyy");

        public static readonly IReadOnlyList<SiteLanguage> All =
        [
            English,
            new("km", "km-KH", "km", "ភាសាខ្មែរ", "ខ្មែរ", "d MMMM yyyy"),
            new("vi", "vi-VN", "vi", "Tiếng Việt", "VI", "d 'tháng' M, yyyy"),
            new("zh-tw", "zh-TW", "zh-Hant-TW", "中文 (台灣)", "中文", "yyyy年M月d日"),
        ];

        public static SiteLanguage? Find(string? code) =>
            All.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// The language of the current request. LanguageMiddleware sets the thread culture from the URL,
        /// so this works anywhere during a request, including in singletons like ContentService.
        /// </summary>
        public static SiteLanguage Current =>
            All.FirstOrDefault(l => l.Culture == CultureInfo.CurrentUICulture.Name) ?? English;
    }
}
