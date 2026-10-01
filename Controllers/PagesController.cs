using Microsoft.AspNetCore.Mvc;

namespace Otech.Controllers
{
    public class PagesController : Controller
    {
        public IActionResult About() => View();
        public IActionResult Contact() => View();
        public IActionResult Error404() => View();
        public IActionResult Faq() => View();
        public IActionResult Pricing() => View();
        public IActionResult Team() => View();
        public IActionResult Testimonial() => View();
    }
}