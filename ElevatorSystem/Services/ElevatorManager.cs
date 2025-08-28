using ElevatorSystem.Controllers;
using ElevatorSystem.Models;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace ElevatorSystem.Services;

/// <summary>
/// Our singleton ElevatorManager instance manages all elevator state and logic (the brain).
/// </summary>
//User/SS           ElevatorSystem API      ElevatorManager     Timer
//   |                   |                       |                |
//   |  POST /request    |                       |                |
//   |------------------>|                       |                |
//   |                   |  .ReceiveRequest(req) |                |
//   |                   |---------------------->|                |
//   |                   |                       |   Add req to   |
//   |                   |                       |   pending queue|
//   |                   |      200 OK           |                |
//   |<------------------|                       |                |
//   |                   |                       |                |
//=== Timer fires(tick) =========================================|
//   |                   |        (AssignRequests)                |
//   |                   |--------------------------------------->|
//   |                   |                       | Assign request |
//   |                   |                       | to elevator    |
//   |                   |<---------------------------------------|
//   |                   |      (Step)             |              |
//   |                   |--------------------------------------->|
//   |                   |                       | Move elevator  |
//   |                   |                       | (advance/dequeue)
//   |                   |<---------------------------------------|
//=== Repeat ===================================                  |
//
//
//API receives elevator request
//   |
//ElevatorManager.ReceiveRequest(added to queue)
//   |
//ElevatorSimulationService tick:
//    -> AssignRequests(assign queued requests)
//    -> Step(move elevators)
//    -> Wait for next tick
//(repeats forever)
public class ElevatorManager
{
    private readonly ILogger<ElevatorManager> _logger;

    private readonly int _floors;
    private readonly int _elevatorCount;

    private readonly List<Elevator> _elevators;
    private readonly Queue<ElevatorRequest> _pendingRequests = new Queue<ElevatorRequest>();

    private readonly object _lock = new object();

    public int Floors => _floors;

    public ElevatorManager(ILogger<ElevatorManager> logger, 
        int floors = 10, 
        int elevatorCount = 4)
    {
        _logger = logger;
        _floors = floors;
        _elevatorCount = elevatorCount;
        _elevators = Enumerable.Range(1, elevatorCount).Select(i => new Elevator(i)).ToList();
    }

    /// <summary>
    /// Called by controller: Add a new elevator request
    /// </summary>
    public void ReceiveRequest(ElevatorRequest request)
    {
        lock (_lock)
        {
            _pendingRequests.Enqueue(request);
            _logger.LogInformation($"\"{request.Direction}\" request on floor {request.Floor} received.");
        }
    }

    /// <summary>
    /// Assign requests to elevators.
    /// More efficient to call this periodically (e.g. every second) like inside a timer or background task.
    /// </summary>
    public void AssignRequests()
    {
        lock (_lock)
        {
            while (_pendingRequests.Count > 0)
            {
                var request = _pendingRequests.Peek();

                // Find all elevators that are idle or heading in request.Direction (and will pass the floor)
                var candidates = _elevators.Where(e =>
                    e.IsIdle ||
                    e.Direction == request.Direction &&
                        (request.Direction == Direction.Up && e.CurrentFloor <= request.Floor ||
                         request.Direction == Direction.Down && e.CurrentFloor >= request.Floor)
                ).ToList();

                if (candidates.Any())
                {
                    // Assign to closest elevator
                    var chosen = candidates.OrderBy(e => Math.Abs(e.CurrentFloor - request.Floor)).First();
                    if (!chosen.TargetFloors.Contains(request.Floor))
                    {
                        chosen.TargetFloors.Enqueue(request.Floor);
                    }
                    // Set the direction if idle
                    if (chosen.Direction == null)
                    {
                        chosen.Direction = request.Floor > chosen.CurrentFloor ? Direction.Up : Direction.Down;
                    }

                    _pendingRequests.Dequeue();

                    _logger.LogInformation($"Elevator {chosen.Id} assigned \"{request.Direction}\" request on floor {request.Floor} (currently at floor {chosen.CurrentFloor})");
                }
                else
                {
                    // No candidate right now; leave request for next tick
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Move all elevators one step (NOTE: 1 step = 1 floor).
    /// Call this on a timer or "tick" (from a background process/service)
    /// </summary>
    public void Step()
    {
        lock (_lock)
        {
            foreach (var elevator in _elevators)
            {
                if (elevator.TargetFloors.Count == 0)
                {
                    elevator.Direction = null;
                    continue;
                }

                var target = elevator.TargetFloors.Peek();

                if (elevator.CurrentFloor == target)
                {
                    // Arrived at floor
                    Console.WriteLine($"Elevator {elevator.Id} stopped at floor {target} for \"{(elevator.Direction == Direction.Up ? "Up" : "Down")}\" request.");

                    elevator.TargetFloors.Dequeue();

                    // Set new direction if any more stops
                    if (elevator.TargetFloors.Count > 0)
                    {
                        var next = elevator.TargetFloors.Peek();
                        elevator.Direction = next > elevator.CurrentFloor ? Direction.Up : Direction.Down;
                    }
                    else
                    {
                        elevator.Direction = null;
                    }
                }
                else
                {
                    // Before stepping up or down, check our current elevator floor for logging.
                    var oldFloor = elevator.CurrentFloor;

                    elevator.Direction = target > elevator.CurrentFloor ? Direction.Up : Direction.Down;
                    elevator.CurrentFloor += elevator.Direction == Direction.Up ? 1 : -1;

                    _logger.LogInformation($"Elevator {elevator.Id} moving {elevator.Direction} from floor {oldFloor} to {elevator.CurrentFloor}. Next stop: floor {target}");
                }
            }
        }
    }

    /// <summary>
    /// Get a snapshot of all elevator statuses (for controller/view)
    /// </summary>
    public List<Elevator> GetElevators()
    {
        lock (_lock)
        {
            // Get a copy of elevators to avoid external mutation.
            return _elevators.Select(e => new Elevator(e)).ToList();
        }
    }

    /// <summary>
    /// For diagnostics: get all pending requests, if needed
    /// </summary>
    public List<ElevatorRequest> GetPendingRequests()
    {
        lock (_lock)
        {
            return _pendingRequests.ToList();
        }
    }
}
