using ElevatorControlSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorControlSystem.Controllers;

/// <summary>
/// POST http://{hostname}:{port}/api/elevator
/// </summary>
[Route("api/elevator")]
[ApiController]
public class ElevatorApiController : ControllerBase
{
    /// <summary>
    /// Handles elevator requests from external clients (Simulation System).
    /// </summary>
    [HttpPost("request")]
    public IActionResult RequestElevator([FromBody] ElevatorRequest request)
    {
        // Logic to handle and queue elevator request here

        return Ok(new { message = "Request received" });
    }
}
