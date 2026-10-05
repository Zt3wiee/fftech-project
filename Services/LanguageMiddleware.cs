using System.Globalization;

namespace Otech.Services
{
    /// <summary>
    /// Picks the language from the URL prefix ("/km/services" is Khmer), strips the prefix so the
    /// normal routes match, and sets the thread culture for the rest of the request.
    /// </summary>
    public class LanguageMiddleware(RequestDelegate next)
    {
        private const string ItemKey = "SiteLanguage";

        public Task InvokeAsync(HttpContext context)
        {
            var language = FromPath(context);

            if (language is null)
            {
                // Error pages are re-executed at /status/404 or /error: keep the language of the original URL.
                if (context.Items[ItemKey] is SiteLanguage original)
                {
                    language = original;
                }
                else
                {
                    language = Languages.English;

                    // A returning visitor who picked another language lands on the homepage in that language.
                    if (HttpMethods.IsGet(context.Request.Method) && context.Request.Path == "/" &&
                        Languages.Find(context.Request.Cookies[Languages.CookieName]) is { IsDefault: false } saved)
                    {
                        context.Response.Headers.CacheControl = "no-cache";
                        context.Response.Redirect(saved.UrlPrefix + context.Request.QueryString);
                        return Task.CompletedTask;
                    }
                }
            }

            context.Items[ItemKey] = language;
            var culture = CultureInfo.GetCultureInfo(language.Culture);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            return next(context);
        }

        private static SiteLanguage? FromPath(HttpContext context)
        {
            foreach (var language in Languages.All.Where(l => !l.IsDefault))
            {
                if (context.Request.Path.StartsWithSegments(language.UrlPrefix, StringComparison.OrdinalIgnoreCase, out var rest))
                {
                    context.Request.Path = rest.HasValue ? rest : "/";
                    return language;
                }
            }
            return null;
        }
    }
}
