using System.Diagnostics;
using System.Linq;
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
        private readonly ElevatorManager _manager;

        public HomeController(ILogger<HomeController> logger,
            ElevatorManager manager)
        {
            _logger = logger;
            _manager = manager;
        }

        public async Task<IActionResult> Index()
        {
            var floors = _manager.Floors;
            var elevatorsViewModel = _manager.GetElevators().Select(e => new ElevatorViewModel
            {
                Id = e.Id,
                CurrentFloor = e.CurrentFloor,
                Direction = e.Direction?.ToString() ?? "", // null = idle
                TargetFloors = e.TargetFloors.ToList(),
                IsIdle = e.IsIdle
            }).ToList();
            var pendingRequestsViewModel = _manager.GetPendingRequests().Select(r => new ElevatorRequestViewModel
            {
                Floor = r.Floor,
                Direction = r.Direction.ToString()
            }).ToList();

            var model = new ElevatorManagerViewModel
            {
                Floors = floors,
                Elevators = elevatorsViewModel,
                PendingRequests = pendingRequestsViewModel
            };

            return View(model);
        }

        public async Task<IActionResult> Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
