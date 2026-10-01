using System.Threading.RateLimiting;
using Otech.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.Configure<ContentOptions>(builder.Configuration.GetSection("Content"));
builder.Services.AddSingleton<ContentService>();

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
app.UseRouting();
app.UseRateLimiter();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers().WithStaticAssets();

// Old template pages (/Landing/..., /Pages/..., etc.) until those files are deleted.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}/{id?}")
    .WithStaticAssets();

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
