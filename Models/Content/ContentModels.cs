namespace Otech.Models.Content
{
    // Every item that can be dummy content carries IsPlaceholder, so it can be
    // found (and hidden with Content:HidePlaceholders) before the site goes live.
    public interface IPlaceholder
    {
        bool IsPlaceholder { get; }
    }

    public record Link(string Label, string Url);

    public record Office(string City, string Label, string Address, string Phone, string? MapEmbedUrl);

    public record SocialLink(string Network, string Icon, string Url);

    public record TitledText(string Title, string Text, string? Icon = null);

    public record ContentBlock(string? Heading, List<string>? Paragraphs, List<string>? Bullets, string? Quote);

    public record Hero(string Eyebrow, string Heading, string Text, Link PrimaryCta, Link? SecondaryCta, List<string> TrustPoints);

    public record AboutContent(
        string Eyebrow,
        string Heading,
        string Intro,
        List<TitledText> Highlights,
        List<string> Story,
        List<TitledText> Pillars);

    public record Cta(string Heading, string Text, Link PrimaryCta, Link? SecondaryCta);

    public record SiteContent(
        bool IsPlaceholder,
        string Name,
        string ShortName,
        string Tagline,
        string MetaDescription,
        string FooterBlurb,
        int FoundedYear,
        string Email,
        string Hours,
        List<Office> Offices,
        List<SocialLink> Social,
        Hero Hero,
        AboutContent About,
        List<TitledText> WhyUs,
        List<TitledText> Process,
        List<TitledText> EngagementModels,
        Cta Cta) : IPlaceholder;

    public record Service(
        string Slug,
        string Title,
        string Icon,
        string Summary,
        string Image,
        List<string> Intro,
        List<string> Benefits,
        List<ContentBlock> Sections,
        List<string> Technologies,
        bool IsPlaceholder) : IPlaceholder;

    public record Industry(
        string Slug,
        string Title,
        string Icon,
        string Summary,
        string Image,
        List<string> Intro,
        List<string> Challenges,
        List<string> HowWeHelp,
        List<string> RelatedServices,
        bool IsPlaceholder) : IPlaceholder;

    public record Metric(string Value, string Label);

    public record WorkItem(
        string Slug,
        string Title,
        // "named" = client agreed to be named, "anonymized" = real project under NDA,
        // "demo" = sample/concept project built to show capability.
        string ProofType,
        string Client,
        string Industry,
        string Region,
        string Year,
        string Summary,
        string Image,
        List<string> Services,
        List<string> Challenge,
        List<string> Solution,
        List<Metric> Results,
        List<string> Technologies,
        bool Featured,
        bool IsPlaceholder) : IPlaceholder
    {
        public string ProofLabel => ProofType switch
        {
            "named" => "Client project",
            "anonymized" => "Client confidential",
            "demo" => "Sample project",
            _ => ProofType
        };
    }

    public record Testimonial(string Quote, string? Name, string Role, string Company, string Region, bool IsPlaceholder) : IPlaceholder;

    public record Stat(int Value, string Suffix, string Label, bool IsPlaceholder) : IPlaceholder;

    public record TeamMember(string Name, string Role, string Location, string Bio, string? Image, string? LinkedIn, bool IsPlaceholder) : IPlaceholder
    {
        public string Initials => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => p[0]));
    }

    public record FaqItem(string Category, string Question, string Answer, bool IsPlaceholder) : IPlaceholder;

    public record Insight(
        string Slug,
        string Title,
        DateOnly Date,
        string Author,
        string Category,
        string Summary,
        string Image,
        List<ContentBlock> Body,
        List<string> Tags,
        bool IsPlaceholder) : IPlaceholder;
}
