using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.WebEncoders;
using Otech.Services;

var builder = WebApplication.CreateBuilder(args);

// Form error messages are translated through the same Localizer as the rest of the site.
builder.Services.AddSingleton<IStringLocalizerFactory, SiteStringLocalizerFactory>();
builder.Services.AddControllersWithViews().AddDataAnnotationsLocalization();
// Write Khmer, Vietnamese and Chinese letters as they are, instead of as &#x...; codes (which make pages several times bigger).
builder.Services.Configure<WebEncoderOptions>(options => options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.Configure<ContentOptions>(builder.Configuration.GetSection("Content"));
builder.Services.AddSingleton<ContentService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<Localizer>();

builder.Services.Configure<ContactOptions>(builder.Configuration.GetSection("Contact"));
builder.Services.AddSingleton<ContactInbox>();

// Limits contact form submissions to 5 per 10 minutes per visitor IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("contact", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10) }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/status/{0}");

app.UseHttpsRedirection();
// Reads the language from the URL (/km/..., /vi/..., /zh-tw/...) before routing sees the path.
app.UseMiddleware<LanguageMiddleware>();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers().WithStaticAssets();

// Warn at startup while dummy content is still on the site.
var placeholders = app.Services.GetRequiredService<ContentService>().CountPlaceholders()
    .Where(kv => kv.Value > 0)
    .Select(kv => $"{kv.Key}: {kv.Value}")
    .ToList();
if (placeholders.Count > 0)
{
    app.Logger.LogWarning("Dummy content still marked isPlaceholder ({Files}). Replace it before launch, or set Content:HidePlaceholders to true.",
        string.Join(", ", placeholders));
}

app.Run();
