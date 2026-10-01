using Otech.Models.Content;

namespace Otech.Models
{
    /// <summary>The dark title banner at the top of every inner page.</summary>
    public record PageBanner(string Title, string? Text = null, params Link[] Crumbs);
}
