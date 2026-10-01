using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Otech.Models.Content;

namespace Otech.Services
{
    public class ContentOptions
    {
        // Set to true before launch to hide every item marked "isPlaceholder": true.
        public bool HidePlaceholders { get; set; }
    }

    /// <summary>
    /// Reads the site's content from the JSON files in /Content. A file is re-read
    /// when it changes on disk, so content edits show up without restarting the site.
    /// </summary>
    public class ContentService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly string _root;
        private readonly bool _hidePlaceholders;
        private readonly ConcurrentDictionary<string, (DateTime Stamp, object Value)> _cache = new();

        public ContentService(IWebHostEnvironment env, IOptions<ContentOptions> options)
        {
            _root = Path.Combine(env.ContentRootPath, "Content");
            _hidePlaceholders = options.Value.HidePlaceholders;
        }

        public SiteContent Site => Load<SiteContent>("site.json");

        public IReadOnlyList<Service> Services => Visible(Load<List<Service>>("services.json"));
        public IReadOnlyList<Industry> Industries => Visible(Load<List<Industry>>("industries.json"));
        public IReadOnlyList<WorkItem> Work => Visible(Load<List<WorkItem>>("work.json"));
        public IReadOnlyList<Testimonial> Testimonials => Visible(Load<List<Testimonial>>("testimonials.json"));
        public IReadOnlyList<Stat> Stats => Visible(Load<List<Stat>>("stats.json"));
        public IReadOnlyList<TeamMember> Team => Visible(Load<List<TeamMember>>("team.json"));
        public IReadOnlyList<FaqItem> Faq => Visible(Load<List<FaqItem>>("faq.json"));
        public IReadOnlyList<Insight> Insights => Visible(Load<List<Insight>>("insights.json")).OrderByDescending(i => i.Date).ToList();

        public Service? GetService(string slug) => Services.FirstOrDefault(s => Same(s.Slug, slug));
        public Industry? GetIndustry(string slug) => Industries.FirstOrDefault(i => Same(i.Slug, slug));
        public WorkItem? GetWork(string slug) => Work.FirstOrDefault(w => Same(w.Slug, slug));
        public Insight? GetInsight(string slug) => Insights.FirstOrDefault(i => Same(i.Slug, slug));

        /// <summary>Number of items still marked as placeholder, per file.</summary>
        public IReadOnlyDictionary<string, int> CountPlaceholders()
        {
            var all = new Dictionary<string, IEnumerable<IPlaceholder>>
            {
                ["site.json"] = new[] { Load<SiteContent>("site.json") },
                ["services.json"] = Load<List<Service>>("services.json"),
                ["industries.json"] = Load<List<Industry>>("industries.json"),
                ["work.json"] = Load<List<WorkItem>>("work.json"),
                ["testimonials.json"] = Load<List<Testimonial>>("testimonials.json"),
                ["stats.json"] = Load<List<Stat>>("stats.json"),
                ["team.json"] = Load<List<TeamMember>>("team.json"),
                ["faq.json"] = Load<List<FaqItem>>("faq.json"),
                ["insights.json"] = Load<List<Insight>>("insights.json"),
            };
            return all.ToDictionary(kv => kv.Key, kv => kv.Value.Count(i => i.IsPlaceholder));
        }

        private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private IReadOnlyList<T> Visible<T>(List<T> items) where T : IPlaceholder =>
            _hidePlaceholders ? items.Where(i => !i.IsPlaceholder).ToList() : items;

        private T Load<T>(string file)
        {
            var path = Path.Combine(_root, file);
            var stamp = File.GetLastWriteTimeUtc(path);
            if (_cache.TryGetValue(file, out var cached) && cached.Stamp == stamp)
                return (T)cached.Value;

            try
            {
                using var stream = File.OpenRead(path);
                var value = JsonSerializer.Deserialize<T>(stream, JsonOptions)
                    ?? throw new InvalidDataException($"Content/{file} is empty.");
                _cache[file] = (stamp, value);
                return value;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Content/{file} is not valid JSON: {ex.Message}", ex);
            }
        }
    }
}
