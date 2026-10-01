using Microsoft.AspNetCore.Mvc;

namespace Otech.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index1() => View();
        public IActionResult Index2() => View();
        public IActionResult Index3() => View();
        public IActionResult Index4() => View();
        public IActionResult Single1() => View();
        public IActionResult Single2() => View();
        public IActionResult Single3() => View();
        public IActionResult Single4() => View();
    }
}