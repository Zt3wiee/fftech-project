using System.ComponentModel.DataAnnotations;

namespace Otech.Models
{
    public class ContactForm
    {
        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(150)]
        public string? Company { get; set; }

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(200)]
        public string Email { get; set; } = "";

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Topic { get; set; }

        [Required(ErrorMessage = "Please tell us a little about your project.")]
        [StringLength(5000, MinimumLength = 10, ErrorMessage = "Please write at least 10 characters.")]
        public string Message { get; set; } = "";

        // Spam trap: hidden from people, so only bots fill it in.
        public string? Website { get; set; }
    }
}
