using ElevatorSystem.Models;
using ElevatorSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorSystem.Controllers;

/// <summary>
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

    /// <summary>
    /// Handles elevator requests from external clients (Simulation System).
    /// </summary>
    [HttpPost("request")]
    public IActionResult RequestElevator([FromBody] ElevatorRequest request)
    {
        _manager.ReceiveRequest(request);

        return Ok(new { message = "Request received" });
    }
}
