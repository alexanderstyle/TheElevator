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
        var hallRequests = _manager.GetAllRequests()
            .Select(r => new { r.Floor, Direction = r.Direction.ToString(), Status = r.Status.ToString(), r.AssignedElevatorId })
            .ToList();
        var elevators = _manager.GetElevators()
            .Select(e => new { e.Id, e.CurrentFloor, direction = e.Direction.ToString(), e.TargetFloors, isIdle = e.IsIdle.ToString() });

        await Task.CompletedTask;

        return Ok(new { floors = _manager.Floors, allRequests = hallRequests, elevators = elevators });
    }

    /// <summary>
    /// Handles elevator requests from external clients (Simulation System).
    /// </summary>
    [HttpPost]
    [Route("request")]
    public async Task<IActionResult> RequestElevator([FromBody] HallRequest request)
    {
        await _manager.ReceiveRequestAsync(request);

        return Ok(new { message = "Api elevator request received" });
    }
}
