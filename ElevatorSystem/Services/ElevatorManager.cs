using ElevatorSystem.Helper;
using ElevatorSystem.Models;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ElevatorSystem.Services;

public class ElevatorManager
{
    private readonly ILogger<ElevatorManager> _logger;
    private readonly List<HallRequest> _hallRequests = new();
    private readonly List<Elevator> _elevators;
    private readonly int _floors;

    public int Floors => _floors;
    public int OnloadingDelayInSeconds { get; set; } = 10;

    public ElevatorManager(ILogger<ElevatorManager> logger, 
        int floors = 10, 
        int elevatorCount = 4)
    {
        _logger = logger;
        _floors = floors;
        _elevators = Enumerable.Range(1, elevatorCount)
                               .Select(i => new Elevator(i, 1)).ToList();
    }

    public async Task ReceiveRequestAsync(HallRequest request)
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

        await Task.CompletedTask;
    }

    public async Task AssignRequestAsync()
    {
        await AssignUpRequestsAsync();

        await AssignDownRequestsAsync();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Increment or decrement each elevator's current floor toward its next target (if any).
    /// </summary>
    public async Task StepAsync()
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
                // Simulate delay due to onloading passengers.
                await Task.Delay(new TimeSpan(0, 0, OnloadingDelayInSeconds));

                _logger.LogInformation($"Onloading for {OnloadingDelayInSeconds} seconds. Car {elevator.Id} is on floor {elevator.CurrentFloor} and {(elevator.Direction?.ToString().ToLower() ?? "idle")}.");

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

    public List<HallRequest> GetAllRequests()
    => _hallRequests.ToList();

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

        // Get the idle elevator that is closest to the lowest pendingDownRequests request floor.
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

    public Elevator? GetClosestIdleElevatorForDownRequest(int floor, Direction direction)
    {
        // For request down and floor is at 8th floor, and all elevators are idle
        // Choose the closest idle elevator
        // For each elevator that are idle
        // If elevator is below request floor or idle above request floor
        // Subtract elevatorDistance  and get the lowest elevatorDistance 
        // Return that elevator.

        Elevator? closestElevator = null;

        foreach (var elevator in _elevators.Where(e => e.IsIdle))
        {

        }

        return closestElevator;
    }

    public Elevator? GetClosestIdleElevatorForDownRequests()
    {
        Elevator? closestElevator = null;

        // Get the idle elevator that is closest to the lowest pendingDownRequests request floor.
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

    /// <summary>
    /// General flow
    /// Get all pending requests
    /// Get candidate elevator
    /// Assign up requests to moving up or idle elevator that is below lowest up requested floor.
    /// Assign
    /// </summary>
    private async Task AssignUpRequestsAsync()
    {
        // Get all pendingDownRequests that are going up -> [5 6 7]. Sort asc. Get the lowest floor request -> 5. 
        // Get all elevators that are moving up or idle that can pass that lowest floor -> elevators 1 to 5.
        // For each elevator, get the lowest elevatorDistance elevator to the lowest elevatorDistance floor request.
        // Assign that elevator. (Add to target floors)
        // Set status to assigned.
        // Assign elevator id.

        var pendingUpRequests = _hallRequests
            .Where(x => x.Status == HallRequestStatus.Pending && x.Direction == Direction.Up)
            .OrderBy(x => x.Floor)
            .ToList();

        if (!pendingUpRequests.Any())
        {
            return;
        }

        var lowestFloorRequest = pendingUpRequests
            .First()
            .Floor;

        // Do we have an elevator that is below the lowest up requests floor that can service the pending requests?
        var elevatorCandidates = _elevators
            .Where(e => (e.CurrentFloor < lowestFloorRequest && e.Direction == Direction.Up) || (e.CurrentFloor < lowestFloorRequest && e.IsIdle))
            .OrderBy(e => e.Id)
            .ToList();

        if (!elevatorCandidates.Any())
        {
            return;
        }

        var lowestDistance = int.MaxValue;
        Elevator? closestElevator = null;

        foreach (var elevator in elevatorCandidates)
        {
            if (lowestFloorRequest > elevator.CurrentFloor)
            {
                var elevatorDistance = lowestFloorRequest - elevator.CurrentFloor;

                // Elevator is closer 
                if (elevatorDistance < lowestDistance)
                {
                    lowestDistance = elevatorDistance;

                    closestElevator = elevator;
                }
            }
        }

        if (closestElevator != null)
        {
            foreach (var pending in pendingUpRequests)
            {
                closestElevator.TargetFloors.Add(pending.Floor);
                closestElevator.Direction = Direction.Up;
                pending.AssignedElevatorId = closestElevator.Id;
                pending.Status = HallRequestStatus.Assigned;
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Assign an elevator to all down requests where the elevator is stationed or moving down above the highest down requested floors.
    /// </summary>
    private async Task AssignDownRequestsAsync()
    {
        var pendingDownRequests = _hallRequests
            .Where(x => x.Status == HallRequestStatus.Pending && x.Direction == Direction.Down)
            .OrderByDescending(x => x.Floor)
            .ToList();

        if (!pendingDownRequests.Any())
        {
            return;
        }

        var highestFloorRequest = pendingDownRequests
            .First()
            .Floor;

        // Do we have an elevator that is above the highest down requests floor that can service these pending down requests?
        var elevatorCandidates = _elevators
            .Where(e => (e.CurrentFloor > highestFloorRequest && e.Direction == Direction.Down) || (e.CurrentFloor > highestFloorRequest && e.IsIdle))
            .OrderBy(e => e.Id)
            .ToList();

        if (!elevatorCandidates.Any())
        {
            return;
        }

        var lowestDistance = int.MaxValue;
        Elevator? closestElevator = null;

        foreach (var elevator in elevatorCandidates)
        {
            if (highestFloorRequest < elevator.CurrentFloor)
            {
                var elevatorDistance = elevator.CurrentFloor - highestFloorRequest;

                // Elevator is closer 
                if (elevatorDistance < lowestDistance)
                {
                    lowestDistance = elevatorDistance;

                    closestElevator = elevator;
                }
            }
        }

        if (closestElevator != null)
        {
            foreach (var pending in pendingDownRequests)
            {
                closestElevator.TargetFloors.Add(pending.Floor);
                closestElevator.Direction = Direction.Down;
                pending.AssignedElevatorId = closestElevator.Id;
                pending.Status = HallRequestStatus.Assigned;
            }
        }

        await Task.CompletedTask;
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

    internal void SetElevatorState(int elevatorId, int currentFloor, Direction? direction)
    {
        var elevator = _elevators
            .Where(e => e.Id == elevatorId)
            .Single();

        elevator.CurrentFloor = currentFloor;
        elevator.Direction = direction;
    }
}