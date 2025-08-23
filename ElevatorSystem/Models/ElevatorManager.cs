using System.Linq;

namespace ElevatorSystem.Models;

public class ElevatorManager
{
    private readonly int _floors;
    private readonly int _elevatorCount;

    private readonly List<Elevator> _elevators;
    private readonly Queue<ElevatorRequest> _pendingRequests = new Queue<ElevatorRequest>();

    private readonly object _lock = new object();

    public ElevatorManager(int floors = 10, int elevatorCount = 4)
    {
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
        }
    }

    /// <summary>
    /// Assign requests to elevators (very simple algorithm)
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
                    (e.Direction == request.Direction &&
                        ((request.Direction == Direction.Up && e.CurrentFloor <= request.Floor) ||
                         (request.Direction == Direction.Down && e.CurrentFloor >= request.Floor)))
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
    /// Move all elevators one step
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
                    elevator.Direction = target > elevator.CurrentFloor ? Direction.Up : Direction.Down;
                    elevator.CurrentFloor += elevator.Direction == Direction.Up ? 1 : -1;
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
