using Microsoft.AspNetCore.Mvc;
using Otech.Services;

namespace Otech.Controllers
{
    // List and detail pages for everything that lives in /Content.

    [Route("services")]
    public class ServicesController(ContentService content) : Controller
    {
        [HttpGet("")]
        public IActionResult Index() => View();

        [HttpGet("{slug}")]
        public IActionResult Detail(string slug)
        {
            var service = content.GetService(slug);
            return service is null ? NotFound() : View(service);
        }
    }

    [Route("industries")]
    public class IndustriesController(ContentService content) : Controller
    {
        [HttpGet("")]
        public IActionResult Index() => View();

        [HttpGet("{slug}")]
        public IActionResult Detail(string slug)
        {
            var industry = content.GetIndustry(slug);
            return industry is null ? NotFound() : View(industry);
        }
    }

    [Route("work")]
    public class WorkController(ContentService content) : Controller
    {
        [HttpGet("")]
        public IActionResult Index() => View();

        [HttpGet("{slug}")]
        public IActionResult Detail(string slug)
        {
            var work = content.GetWork(slug);
            return work is null ? NotFound() : View(work);
        }
    }

    [Route("insights")]
    public class InsightsController(ContentService content) : Controller
    {
        [HttpGet("")]
        public IActionResult Index() => View();

        [HttpGet("{slug}")]
        public IActionResult Detail(string slug)
        {
            var insight = content.GetInsight(slug);
            return insight is null ? NotFound() : View(insight);
        }
    }
}
