using ElevatorSystem.Helper;
using ElevatorSystem.Models;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

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
        BatchOnTheWayRequestsOnGoingUpElevators();

        BatchPendingUpRequestsToIdleElevator();

        BatchPendingDownRequestsToIdleElevator();

        // TODO: Add more advanced use cases or rules/ policy handlers for assignment as needed.
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

            // Always proceed to the next target in direction order (thanks to InsertTargetFloorInDirectionOrder)
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

    public List<HallRequest> GetAllPendingRequests()
        => _hallRequests.Where(x => x.Status == HallRequestStatus.Pending).ToList();

    public List<HallRequest> GetPendingUpRequests()
    => _hallRequests.Where(x => x.Status == HallRequestStatus.Pending && x.Direction == Direction.Up).ToList();

    public List<HallRequest> GetPendingDownRequests()
=> _hallRequests.Where(x => x.Status == HallRequestStatus.Pending && x.Direction == Direction.Down).ToList();

    public List<HallRequest> GetAssignedRequests()
        => _hallRequests.Where(x => x.Status == HallRequestStatus.Assigned).ToList();

    public Elevator? GetClosestIdleElevatorForUpRequests()
    {
        Elevator? closestElevator = null;

        // Get the idle elevator that is closest to the lowest pending request floor.
        foreach (var request in GetPendingUpRequests())
        {
            int minDistance = int.MaxValue;

            // Loop through each elevator to find the closest one
            foreach (var elevator in _elevators.Where(e => e.IsIdle))
            {
                int currentDistance = Math.Abs(elevator.CurrentFloor - request.Floor);

                if (currentDistance < minDistance)
                {
                    minDistance = currentDistance;

                    // Choose that elevator.
                    closestElevator = elevator;
                }
            }
        }

        return closestElevator;
    }

    public Elevator? GetClosestIdleElevatorForDownRequests()
    {
        Elevator? closestElevator = null;

        // Get the idle elevator that is closest to the lowest pending request floor.
        foreach (var request in GetPendingDownRequests())
        {
            int minDistance = int.MaxValue;

            // Loop through each elevator to find the closest one
            foreach (var elevator in _elevators.Where(e => e.IsIdle))
            {
                int currentDistance = Math.Abs(elevator.CurrentFloor - request.Floor);

                if (currentDistance < minDistance)
                {
                    minDistance = currentDistance;

                    // Choose that elevator.
                    closestElevator = elevator;
                }
            }
        }

        return closestElevator;
    }

    private void BatchPendingUpRequestsToIdleElevator()
    {
        Elevator? closestElevator = null;

        // Get all pending requests that are up and sort asc.
        var pendingUpRequests = _hallRequests
            .Where(x => x.Status == HallRequestStatus.Pending && x.Direction == Direction.Up)
            .OrderBy(x => x.Floor)
            .ToList();

        closestElevator = GetClosestIdleElevatorForUpRequests();

        // Impossible to have no closest elevator
        if (closestElevator != null)
        {
            // Assign all pending requests to the closest elevator.
            foreach (var r in pendingUpRequests)
            {
                ElevatorRouteHelper.InsertTargetFloorInDirectionOrder(closestElevator, r.Floor);

                r.Status = HallRequestStatus.Assigned;
                r.AssignedElevatorId = closestElevator.Id;
            }

            closestElevator.Direction = Direction.Up;
        }
    }

    private void BatchPendingDownRequestsToIdleElevator()
    {
        Elevator? closestElevator = null;

        // Get all pending requests that are down and sort asc.
        var pendingUpRequests = _hallRequests
            .Where(x => x.Status == HallRequestStatus.Pending && x.Direction == Direction.Down)
            .OrderByDescending(x => x.Floor) // Descending so we assign top to bottom floors.
            .ToList();

        closestElevator = GetClosestIdleElevatorForDownRequests();

        // Impossible to have no closest elevator
        if (closestElevator != null)
        {
            // Assign all pending requests to the closest elevator.
            foreach (var r in pendingUpRequests)
            {
                ElevatorRouteHelper.InsertTargetFloorInDirectionOrder(closestElevator, r.Floor);

                r.Status = HallRequestStatus.Assigned;
                r.AssignedElevatorId = closestElevator.Id;
            }

            closestElevator.Direction = Direction.Down;
        }
    }

    /// <summary>
    /// Use case: Going up elevator on floor 2. Up request 6 7 8. 6 7 8 added to target floors and elevator is on its way. 
    /// On its way, new request on floor 4 5.
    /// This method should assign all requests 4 5 6 7 9 on first elevator (going up) and should stop at each floor.
    /// Once serviced, Step removes the floor on elevator target floors.
    /// </summary>
    private void BatchOnTheWayRequestsOnGoingUpElevators()
    {
        // Get all requests that have not been assigned yet (pending).
        // If no more to service, just exit.
        // Get going up elevators in order (not idle).
        // (What is the closest elevator that can service the lowest floor request.)
        // Get the closest elevator to the lowest requested floor.
        // For each pending up, assign that closest elevator.
        // Insert target floor in direction order.
        // Set the elevator id of this to the assigned elevator id of the request.
        // Set status to assigned.
        // Set direction to up. (Still moving up).

        var pendingRequests = _hallRequests
                .Where(r => r.Status.Equals(HallRequestStatus.Pending) && r.Direction == Direction.Up)
                .OrderBy(r => r.Floor)
                .ToList();

        // If no more to service, just exit.
        if (!pendingRequests.Any())
        {
            return;
        }

        // (What is the closest elevator that can service the lowest floor requests.)
        var lowestDistance = int.MaxValue;
        Elevator? closest = null; ;

        var upElevators = _elevators
            .Where(e => e.Direction == Direction.Up)
            .OrderBy(e => e.Id)
            .ToList();

        // Get the closest elevator moving up to the lowest floor up request.
        foreach (var e in upElevators)
        {
            var lowestRequestedFloorGoingUp = pendingRequests
                .First()
                .Floor;

            var distance = Math.Abs(lowestRequestedFloorGoingUp - e.CurrentFloor);

            if (distance < lowestDistance)
            {
                lowestDistance = distance;

                closest = e;
            }
        }

        // We now have the closest elevator, assign it.
        var pendingUp = _hallRequests
            .Where(r => r.Status.Equals(HallRequestStatus.Pending) && r.Direction == Direction.Up)
            .OrderBy(r => r.Floor)
            .ToList();

        foreach (var request in pendingUp)
        {
            if (closest != null)
            {
                ElevatorRouteHelper.InsertTargetFloorInDirectionOrder(closest, request.Floor);

                request.Status = HallRequestStatus.Assigned;
                request.AssignedElevatorId = closest.Id;

                _logger.LogInformation($"[Batching] Assigned on-the-way {request.Direction} request @ floor {request.Floor} to Elevator {closest.Id}");
            }
        }
    }

    // Test helper functions only
    internal void ClearAndInjectRequests(List<HallRequest> requests)
    {
        _hallRequests.Clear();
        _hallRequests.AddRange(requests);
    }

    internal Elevator GetActualElevatorById(int id) => _elevators.First(e => e.Id == id);

    internal void SetElevatorCurrentFloor(int elevatorId, int floor)
    {
        var elevator = _elevators
            .Where(e => e.Id == elevatorId)
            .Single();

        elevator.CurrentFloor = floor;
    }

    internal void SetElevatorState(int elevatorId, int currentFloor, Direction direction)
    {
        var elevator = _elevators
            .Where(e => e.Id == elevatorId)
            .Single();

        elevator.CurrentFloor = currentFloor;
        elevator.Direction = direction;
    }
}