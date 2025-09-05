using ElevatorSystem.Helper;
using ElevatorSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class ElevatorManager
{
    private readonly ILogger<ElevatorManager> _logger;
    private readonly List<HallRequest> _hallRequests = new();
    private readonly List<Elevator> _elevators;
    private readonly int _floors;

    public int Floors => _floors;

    public ElevatorManager(ILogger<ElevatorManager> logger, 
        int floors = 10, 
        int elevatorCount = 4)
    {
        _logger = logger;
        _floors = floors;
        _elevators = Enumerable.Range(1, elevatorCount)
                               .Select(i => new Elevator(i, 1)).ToList();
    }

    // -- Step 1: Human hall pendingRequest, prevents duplicates (same floor+direction)
    public void ReceiveRequest(HallRequest request)
    {
        // Cannot go up from top floor
        if (request.Floor == Floors && request.Direction == Direction.Up)
            return;

        // Cannot go down from bottom floor
        if (request.Floor == 1 && request.Direction == Direction.Down)
            return;

        // Check for duplicate. Already exists with same floor and direction and not yet served
        bool exists = _hallRequests.Any(r =>
            r.Floor == request.Floor &&
            r.Direction == request.Direction &&
            r.Status != HallRequestStatus.Assigned
        );

        if (exists)
            return;

        // Add the new pendingRequest
        _hallRequests.Add(new HallRequest(request.Floor, request.Direction));

        _logger.LogInformation($"\"{request.Direction}\" request on floor {request.Floor} received.");
    }

    public void AssignRequests()
    {
        var pendingRequests = _hallRequests
            .Where(r => r.Status == HallRequestStatus.Pending)
            .OrderBy(r => r.Floor)
            .ToList();

        foreach (var pendingRequest in pendingRequests)
        {
            // Try to batch with any moving elevator in the same direction that will pass requested floor.
            var passingElevator = _elevators
                .Where(e =>
                    e.Direction == pendingRequest.Direction &&
                    ElevatorRouteHelper.IsPassingFloor(e, pendingRequest.Floor))
                .OrderBy(e => Math.Abs(e.CurrentFloor - pendingRequest.Floor))
                .FirstOrDefault();

            if (passingElevator != null)
            {
                ElevatorRouteHelper.InsertFloorInDirectionOrder(passingElevator, pendingRequest.Floor);

                pendingRequest.Status = HallRequestStatus.Assigned;
                pendingRequest.AssignedElevatorId = passingElevator.Id;

                continue; 
            }

            // Otherwise, assign to nearest idle elevator
            var idleElevator = _elevators
                .Where(e => e.IsIdle)
                .OrderBy(e => Math.Abs(e.CurrentFloor - pendingRequest.Floor))
                .FirstOrDefault();

            if (idleElevator != null)
            {
                ElevatorRouteHelper.InsertFloorInDirectionOrder(idleElevator, pendingRequest.Floor);

                idleElevator.Direction = pendingRequest.Floor > idleElevator.CurrentFloor ? Direction.Up : Direction.Down;
                pendingRequest.Status = HallRequestStatus.Assigned;
                pendingRequest.AssignedElevatorId = idleElevator.Id;
            }

            // No candidate elevator found, will try again next time.
        }
    }

    /// <summary>
    /// Increment or decrement each elevator's current floor toward its next target (if any).
    /// </summary>
    public void Step()
    {
        foreach (var elevator in _elevators)
        {
            if (elevator.TargetFloors.Count == 0)
            {
                // No more targets, elevator is idle
                elevator.Direction = null;
                continue;
            }

            // Always proceed to the next target in direction order (thanks to InsertFloorInDirectionOrder)
            var target = elevator.TargetFloors[0];

            if (elevator.CurrentFloor == target)
            {
                // Arrived at target floor!
                elevator.TargetFloors.RemoveAt(0);

                // Remove all assigned hall requests at this floor + this direction + this elevator
                _hallRequests.RemoveAll(r =>
                    r.Floor == elevator.CurrentFloor &&
                    r.Status == HallRequestStatus.Assigned &&
                    r.AssignedElevatorId == elevator.Id &&
                    elevator.Direction == r.Direction
                );

                // Set direction to next target, or set to idle (null) if no targets left
                if (elevator.TargetFloors.Count > 0)
                {
                    var next = elevator.TargetFloors[0];
                    elevator.Direction = next > elevator.CurrentFloor ? Direction.Up : Direction.Down;
                }
                else
                {
                    elevator.Direction = null;
                }

                _logger.LogInformation($"Car {elevator.Id} is on floor {elevator.CurrentFloor} and {(elevator.Direction?.ToString().ToLower() ?? "idle")}.");
            }
            else
            {
                // Move elevator one floor toward next target
                if (target > elevator.CurrentFloor)
                {
                    elevator.Direction = Direction.Up;
                    elevator.CurrentFloor += 1;
                }
                else if (target < elevator.CurrentFloor)
                {
                    elevator.Direction = Direction.Down;
                    elevator.CurrentFloor -= 1;
                }

                _logger.LogInformation($"Car {elevator.Id} is on floor {elevator.CurrentFloor} and {elevator.Direction?.ToString().ToLower()}.");
            }
        }
    }

    /// <summary>
    /// Get a snapshot of all elevator statuses (for controller/view)
    /// </summary>
    public List<Elevator> GetElevators() => _elevators.Select(e => new Elevator(e)).ToList();

    public List<HallRequest> GetPendingRequests()
        => _hallRequests.Where(x => x.Status == HallRequestStatus.Pending).ToList();

    public List<HallRequest> GetAssignedRequests()
        => _hallRequests.Where(x => x.Status == HallRequestStatus.Assigned).ToList();
}