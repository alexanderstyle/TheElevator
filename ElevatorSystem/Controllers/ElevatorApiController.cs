using ElevatorSystem.Models;
using ElevatorSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorSystem.Controllers;

/// <summary>
/// Auxiliary API controller to handle elevator requests from external clients (e.g., Simulation System).
/// This is not stated in requirements but is provided for manual elevator requests via api requests.
/// POST http://{hostname}:{port}/api/elevator
/// </summary>
[Route("api/elevator")]
[ApiController]
public class ElevatorApiController : ControllerBase
{
    private readonly ElevatorManager _manager;

    public ElevatorApiController(ElevatorManager manager)
    {
         _manager = manager;
    }

    [HttpGet]
    [Route("status")]
    public async Task<IActionResult> GetStatus()
    {
        var pendingRequests = _manager.GetAllPendingRequests()
            .Select(r => new { r.Floor, Direction = r.Direction.ToString(), r.Status, r.AssignedElevatorId })
            .ToList();
        var elevators = _manager.GetElevators();

        await Task.CompletedTask;

        return Ok(new { floors = _manager.Floors, pendingRequests = pendingRequests, elevators = elevators });
    }

    /// <summary>
    /// Handles elevator requests from external clients (Simulation System).
    /// </summary>
    [Obsolete("Manual request via this api will be removed in future versions. Use auto-generated requests using background services.")]
    [HttpPost]
    [Route("request")]
    public IActionResult RequestElevator([FromBody] HallRequest request)
    {
        _manager.ReceiveRequest(request);

        return Ok(new { message = "Api elevator request received" });
    }
}
