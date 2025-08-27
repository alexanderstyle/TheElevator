using System.Diagnostics;
using ElevatorSystem.Models;
using ElevatorSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorSystem.Controllers
{
    /// <summary>
    /// Controller for handling home page (our elevator UI) and error views.
    /// Although not required, since logging is in place and describes elevator operations,
    /// I will enhance the UI to show elevator status and operations.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
