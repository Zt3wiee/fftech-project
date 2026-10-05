using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Otech.Models;

namespace Otech.Services
{
    public class ContactOptions
    {
        public string SaveFolder { get; set; } = "App_Data/messages";
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string? SmtpUser { get; set; }
        public string? SmtpPassword { get; set; }
        public bool UseSsl { get; set; } = true;
        public string? FromAddress { get; set; }
        public string? ToAddress { get; set; }

        public bool EmailEnabled =>
            !string.IsNullOrWhiteSpace(SmtpHost) &&
            !string.IsNullOrWhiteSpace(FromAddress) &&
            !string.IsNullOrWhiteSpace(ToAddress);
    }

    /// <summary>
    /// Stores every contact form message as a JSON file, and emails it when SMTP is configured.
    /// Saving first means no message is lost if the email server is down.
    /// </summary>
    public class ContactInbox
    {
        private readonly ContactOptions _options;
        private readonly string _folder;
        private readonly ILogger<ContactInbox> _logger;

        public ContactInbox(IOptions<ContactOptions> options, IWebHostEnvironment env, ILogger<ContactInbox> logger)
        {
            _options = options.Value;
            _folder = Path.Combine(env.ContentRootPath, _options.SaveFolder);
            _logger = logger;
        }

        public async Task SubmitAsync(ContactForm form, string? ipAddress, string language)
        {
            var receivedAt = DateTimeOffset.UtcNow;

            Directory.CreateDirectory(_folder);
            var file = Path.Combine(_folder, $"{receivedAt:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
            var record = new { receivedAt, ipAddress, language, form.Name, form.Company, form.Email, form.Phone, form.Topic, form.Message };
            await File.WriteAllTextAsync(file, JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
            _logger.LogInformation("Contact message from {Email} saved to {File}", form.Email, file);

            if (!_options.EmailEnabled)
                return;

            try
            {
                await SendEmailAsync(form, receivedAt, language);
            }
            catch (Exception ex)
            {
                // The message is already saved, so the visitor still gets a success page.
                _logger.LogError(ex, "Contact message saved but email could not be sent ({File})", file);
            }
        }

        private async Task SendEmailAsync(ContactForm form, DateTimeOffset receivedAt, string language)
        {
            var body = new StringBuilder()
                .AppendLine($"Name:     {form.Name}")
                .AppendLine($"Company:  {form.Company}")
                .AppendLine($"Email:    {form.Email}")
                .AppendLine($"Phone:    {form.Phone}")
                .AppendLine($"Topic:    {form.Topic}")
                .AppendLine($"Language: {language}")
                .AppendLine($"Received: {receivedAt:yyyy-MM-dd HH:mm} UTC")
                .AppendLine()
                .AppendLine(form.Message)
                .ToString();

            using var message = new MailMessage(_options.FromAddress!, _options.ToAddress!)
            {
                Subject = $"Website enquiry: {form.Name}" + (string.IsNullOrWhiteSpace(form.Company) ? "" : $" ({form.Company})"),
                Body = body
            };
            message.ReplyToList.Add(new MailAddress(form.Email, form.Name));

            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort) { EnableSsl = _options.UseSsl };
            if (!string.IsNullOrWhiteSpace(_options.SmtpUser))
                client.Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword);

            await client.SendMailAsync(message);
        }
    }
}
