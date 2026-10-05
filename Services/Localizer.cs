using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Localization;

namespace Otech.Services
{
    /// <summary>
    /// Interface text and links in the current language. In views it is available as L:
    /// L["Get started"] translates a piece of text (it shows the English when there is no
    /// translation yet), and L.Url("/services") keeps a link in the visitor's language.
    /// </summary>
    public class Localizer(ContentService content, IHttpContextAccessor http)
    {
        public SiteLanguage Language => Languages.Current;

        public string this[string english] =>
            content.UiText(Language).TryGetValue(english, out var text) && !string.IsNullOrWhiteSpace(text) ? text : english;

        /// <summary>Translates text with {0}-style placeholders, then fills them in.</summary>
        public string this[string english, params object[] args] => string.Format(this[english], args);

        /// <summary>Adds the language prefix to a link inside the site ("/contact" becomes "/km/contact").</summary>
        public string Url(string url) => Url(url, Language);

        public static string Url(string url, SiteLanguage language)
        {
            // Leave external, mailto:, tel: and #anchor links alone.
            if (language.IsDefault || !url.StartsWith('/') || url.StartsWith("//"))
                return url;
            return url == "/" ? language.UrlPrefix : language.UrlPrefix + url;
        }

        /// <summary>The page being viewed, in another language (for the language switcher and hreflang tags).</summary>
        public string UrlFor(SiteLanguage language, bool includeQuery = true)
        {
            var context = http.HttpContext!;
            // On an error page, link to the page the visitor asked for, not to /status/404.
            var reExecute = context.Features.Get<IStatusCodeReExecuteFeature>();
            var path = reExecute?.OriginalPath ?? context.Request.Path.Value ?? "/";
            var query = reExecute?.OriginalQueryString ?? context.Request.QueryString.Value;
            return Url(path, language) + (includeQuery ? query : "");
        }
    }

    /// <summary>Lets the [Required(ErrorMessage = "...")] messages on forms use the same translations.</summary>
    public class DataAnnotationsLocalizer(Localizer localizer) : IStringLocalizer
    {
        public LocalizedString this[string name] => new(name, localizer[name]);

        public LocalizedString this[string name, params object[] arguments] => new(name, localizer[name, arguments]);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    public class SiteStringLocalizerFactory(Localizer localizer) : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource) => new DataAnnotationsLocalizer(localizer);

        public IStringLocalizer Create(string baseName, string location) => new DataAnnotationsLocalizer(localizer);
    }
}
