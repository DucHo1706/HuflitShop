using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HuflitShopCore.Controllers
{
    public class AboutController : Controller
    {
        private readonly ILogger<AboutController> _logger;

        public AboutController(ILogger<AboutController> logger)
        {
            _logger = logger;
        }

        public IActionResult About()
        {
            return View();
        }
    }
}
