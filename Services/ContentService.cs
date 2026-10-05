using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    ///
    /// Translations live in a folder per language (Content/km/services.json). A translation file
    /// only needs the text: it is laid over the English file, so anything it leaves out
    /// (images, icons, a whole item) falls back to English.
    /// </summary>
    public class ContentService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly JsonDocumentOptions DocumentOptions = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly IReadOnlyDictionary<string, string> NoText = new Dictionary<string, string>();

        private readonly string _root;
        private readonly bool _hidePlaceholders;
        private readonly ConcurrentDictionary<string, (DateTime Stamp, DateTime TranslationStamp, object Value)> _cache = new();

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

        /// <summary>Services in English, whatever the page language (contact form topics are stored in English).</summary>
        public IReadOnlyList<Service> EnglishServices => Visible(Load<List<Service>>("services.json", Languages.English));

        public Service? GetService(string slug) => Services.FirstOrDefault(s => Same(s.Slug, slug));
        public Industry? GetIndustry(string slug) => Industries.FirstOrDefault(i => Same(i.Slug, slug));
        public WorkItem? GetWork(string slug) => Work.FirstOrDefault(w => Same(w.Slug, slug));
        public Insight? GetInsight(string slug) => Insights.FirstOrDefault(i => Same(i.Slug, slug));

        /// <summary>
        /// Interface text (menus, buttons, labels) for a language, from Content/{code}/ui.json,
        /// keyed by the English text. Empty for English.
        /// </summary>
        public IReadOnlyDictionary<string, string> UiText(SiteLanguage language)
        {
            if (language.IsDefault || !File.Exists(Path.Combine(_root, language.Code, "ui.json")))
                return NoText;
            return Load<Dictionary<string, string>>("ui.json", language, translationOnly: true);
        }

        /// <summary>Number of items still marked as placeholder, per file.</summary>
        public IReadOnlyDictionary<string, int> CountPlaceholders()
        {
            var en = Languages.English;
            var all = new Dictionary<string, IEnumerable<IPlaceholder>>
            {
                ["site.json"] = new[] { Load<SiteContent>("site.json", en) },
                ["services.json"] = Load<List<Service>>("services.json", en),
                ["industries.json"] = Load<List<Industry>>("industries.json", en),
                ["work.json"] = Load<List<WorkItem>>("work.json", en),
                ["testimonials.json"] = Load<List<Testimonial>>("testimonials.json", en),
                ["stats.json"] = Load<List<Stat>>("stats.json", en),
                ["team.json"] = Load<List<TeamMember>>("team.json", en),
                ["faq.json"] = Load<List<FaqItem>>("faq.json", en),
                ["insights.json"] = Load<List<Insight>>("insights.json", en),
            };
            return all.ToDictionary(kv => kv.Key, kv => kv.Value.Count(i => i.IsPlaceholder));
        }

        private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private IReadOnlyList<T> Visible<T>(List<T> items) where T : IPlaceholder =>
            _hidePlaceholders ? items.Where(i => !i.IsPlaceholder).ToList() : items;

        private T Load<T>(string file) => Load<T>(file, Languages.Current);

        private T Load<T>(string file, SiteLanguage language, bool translationOnly = false)
        {
            var englishFile = translationOnly ? null : file;
            var translatedFile = language.IsDefault ? null : $"{language.Code}/{file}";

            var stamp = englishFile is null ? default : File.GetLastWriteTimeUtc(Path.Combine(_root, englishFile));
            var translationStamp = translatedFile is null ? default : File.GetLastWriteTimeUtc(Path.Combine(_root, translatedFile));
            var key = $"{language.Code}/{file}";
            if (_cache.TryGetValue(key, out var cached) && cached.Stamp == stamp && cached.TranslationStamp == translationStamp)
                return (T)cached.Value;

            var node = englishFile is null ? null : ReadNode(englishFile);
            if (translatedFile is not null && File.Exists(Path.Combine(_root, translatedFile)))
            {
                var translation = ReadNode(translatedFile);
                node = node is null ? translation : Overlay(node, translation);
            }

            var value = node.Deserialize<T>(JsonOptions)
                ?? throw new InvalidDataException($"Content/{translatedFile ?? englishFile} is empty.");
            _cache[key] = (stamp, translationStamp, value);
            return value;
        }

        private JsonNode? ReadNode(string file)
        {
            try
            {
                using var stream = File.OpenRead(Path.Combine(_root, file));
                return JsonNode.Parse(stream, documentOptions: DocumentOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Content/{file} is not valid JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lays a translation over the English content. Objects are merged property by property.
        /// Lists of items are matched by "slug" when they have one, otherwise by position.
        /// Lists of plain text (paragraphs, bullets) are replaced as a whole.
        /// </summary>
        private static JsonNode? Overlay(JsonNode? english, JsonNode? translation)
        {
            switch (english, translation)
            {
                case (JsonObject target, JsonObject source):
                    foreach (var (name, value) in source)
                    {
                        var existing = target.FirstOrDefault(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase));
                        var targetName = existing.Key ?? name;
                        target[targetName] = Overlay(existing.Value?.DeepClone(), value);
                    }
                    return target;

                case (JsonArray target, JsonArray source) when target.Count > 0 && target.All(i => i is JsonObject):
                    for (var i = 0; i < source.Count; i++)
                    {
                        if (source[i] is not JsonObject item)
                            continue;
                        var slug = item["slug"]?.GetValue<string>();
                        var index = slug is null
                            ? (i < target.Count ? i : -1)
                            : target.ToList().FindIndex(t => t?["slug"]?.GetValue<string>() == slug);
                        if (index >= 0)
                            target[index] = Overlay(target[index]!.DeepClone(), item);
                    }
                    return target;

                default:
                    return translation?.DeepClone();
            }
        }
    }
}
