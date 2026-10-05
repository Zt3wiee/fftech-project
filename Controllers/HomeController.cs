using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Otech.Models;
using Otech.Services;

namespace Otech.Controllers
{
    public class HomeController : Controller
    {
        private readonly ContentService _content;
        private readonly ContactInbox _inbox;
        private readonly Localizer _localizer;

        public HomeController(ContentService content, ContactInbox inbox, Localizer localizer)
        {
            _content = content;
            _inbox = inbox;
            _localizer = localizer;
        }

        [HttpGet("")]
        public IActionResult Index() => View();

        [HttpGet("about")]
        public IActionResult About() => View();

        [HttpGet("team")]
        public IActionResult Team() => _content.Team.Count == 0 ? NotFound() : View();

        [HttpGet("testimonials")]
        public IActionResult Testimonials() => _content.Testimonials.Count == 0 ? NotFound() : View();

        [HttpGet("faq")]
        public IActionResult Faq() => View();

        [HttpGet("contact")]
        public IActionResult Contact(string? topic)
        {
            ViewBag.Sent = TempData["ContactSent"] is true;
            return View(new ContactForm { Topic = topic });
        }

        [HttpPost("contact")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("contact")]
        public async Task<IActionResult> Contact(ContactForm form)
        {
            // Bots fill in the hidden "Website" field. Pretend it worked so they don't retry.
            if (!string.IsNullOrEmpty(form.Website))
            {
                TempData["ContactSent"] = true;
                return Redirect(_localizer.Url("/contact"));
            }

            if (!ModelState.IsValid)
                return View(form);

            await _inbox.SubmitAsync(form, HttpContext.Connection.RemoteIpAddress?.ToString(), System.Globalization.CultureInfo.CurrentUICulture.EnglishName);
            TempData["ContactSent"] = true;
            return Redirect(_localizer.Url("/contact"));
        }

        // Shown for error status codes (404, 400, 429...). The original status code is kept.
        [Route("status/{code:int}")]
        public IActionResult Status(int code)
        {
            ViewBag.StatusCode = code;
            return View(code == StatusCodes.Status404NotFound ? "NotFound" : "Error");
        }

        [Route("error")]
        public IActionResult Error() => View();
    }
}
