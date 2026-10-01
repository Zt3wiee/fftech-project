using Microsoft.AspNetCore.Mvc;

namespace Otech.Controllers
{
    public class ServiceController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult Left() => View();
        public IActionResult Right() => View();
        public IActionResult Single() => View();
    }
}