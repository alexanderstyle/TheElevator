using Castle.Core.Logging;
using ElevatorSystem.Models;
using ElevatorSystem.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElevatorSystem.Tests;

public class ElevatorTest
{
    private Mock<ILogger<ElevatorManager>> _mockLogger = new Mock<ILogger<ElevatorManager>>();

    [Fact]
    public void ReceiveRequest_Should_AddRequestToQueue()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var request = new HallRequest(5, Direction.Down);

        // Act
        manager.ReceiveRequest(request);
        var pending = manager.GetPendingRequests();

        // Assert
        Assert.Single(pending);
        Assert.Equal(5, pending[0].Floor);
        Assert.Equal(Direction.Down, pending[0].Direction);
    }

    [Fact]
    public void ReceiveRequest_Should_AddMultipleRequestToQueue()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var request = new HallRequest(5, Direction.Down);
        var request1 = new HallRequest(8, Direction.Down);
        var request2 = new HallRequest(2, Direction.Up);

        // Act
        manager.ReceiveRequest(request);
        manager.ReceiveRequest(request1);
        manager.ReceiveRequest(request2);
        var pending = manager.GetPendingRequests();

        // Assert
        Assert.Equal(3, pending.Count);
        Assert.Equal(5, pending[0].Floor);
        Assert.Equal(8, pending[1].Floor);
        Assert.Equal(2, pending[2].Floor);
        Assert.Equal(Direction.Down, pending[0].Direction);
        Assert.Equal(Direction.Down, pending[1].Direction);
        Assert.Equal(Direction.Up, pending[2].Direction);

        // All statuses should be Pending.
        Assert.All(pending, r => Assert.Equal(HallRequestStatus.Pending, r.Status));
    }

    [Fact]
    public void ReceiveRequest_Should_AddMultipleRequestsInQueueAndInOrder()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var req1 = new HallRequest(7, Direction.Down);
        var req2 = new HallRequest(2, Direction.Up);
        var req3 = new HallRequest(4, Direction.Down);

        // Act
        manager.ReceiveRequest(req1);
        manager.ReceiveRequest(req2);
        manager.ReceiveRequest(req3);

        var pending = manager.GetPendingRequests();

        // Assert
        Assert.Equal(3, pending.Count);
        Assert.Equal(7, pending[0].Floor);        // FIFO order check
        Assert.Equal(2, pending[1].Floor);
        Assert.Equal(4, pending[2].Floor);
        Assert.Equal(Direction.Down, pending[0].Direction);
        Assert.Equal(Direction.Up, pending[1].Direction);
        Assert.Equal(Direction.Down, pending[2].Direction);

        Assert.All(pending, x => Assert.Equal(HallRequestStatus.Pending, x.Status));
    }

    [Fact]
    public void ReceiveRequests_Should_NotAssignIdenticalRequestToElevator()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);
        var request = new HallRequest(5, Direction.Down);
        var request1 = new HallRequest(5, Direction.Down);
        var request2 = new HallRequest(8, Direction.Down);

        // Act
        manager.ReceiveRequest(request);
        manager.ReceiveRequest(request1);
        manager.ReceiveRequest(request2);

        var pending = manager.GetPendingRequests();

        // Assert
        Assert.Equal(5, pending[0].Floor);
        Assert.Equal(8, pending[1].Floor);
    }

    [Fact]
    public void AssignRequests_Should_AssignRequestToElevator()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);
        var request = new HallRequest(5, Direction.Down);

        // Act
        manager.ReceiveRequest(request);
        manager.AssignRequests();

        var elevators = manager.GetElevators();
        var assignedElevator = elevators.FirstOrDefault(e => e.TargetFloors.Contains(5));

        // Assert
        Assert.NotNull(assignedElevator);
        Assert.Single(assignedElevator.TargetFloors.Select(x => x == 5));
        Assert.Equal(5, assignedElevator.TargetFloors.First());

        // No more pending requests, but there should be assigned.
        Assert.Empty(manager.GetPendingRequests());
        Assert.Single(manager.GetAssignedRequests());
    }

    [Fact]
    public void AssignRequests_BatchesUpRequestsToSingleElevator()
    {
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        // Idle elevator at floor 1 will be assigned all "Up" requests above floor 1
        manager.ReceiveRequest(new HallRequest(3, Direction.Up));
        manager.ReceiveRequest(new HallRequest(5, Direction.Up));
        manager.ReceiveRequest(new HallRequest(7, Direction.Up));

        // Act
        manager.AssignRequests();

        // Assert
        var elevators = manager.GetElevators();
        var elevator = elevators.First();

        // Elevator's targets should be all requested floors (may be in any up order, often insertion order)
        var targets = elevator.TargetFloors.ToList();
        Assert.Contains(3, targets);
        Assert.Contains(5, targets);
        Assert.Contains(7, targets);
        Assert.Equal(Direction.Up, elevator.Direction);

        // Each request should be assigned
        var assigned = manager.GetPendingRequests().Where(r => r.Status == HallRequestStatus.Assigned).ToList();
        Assert.Equal(3, assigned.Count);
        Assert.All(assigned, r => Assert.Equal(elevator.Id, r.AssignedElevatorId));
    }

    [Fact]
    public void AssignRequests_BatchingThenIdle()
    {
        // Arrange: two elevators, both idle on different floors
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 2);
        var elevs = manager.GetElevators();
        // Place elevator 1 at 1, elevator 2 at 10
        elevs[0].CurrentFloor = 1; // id 1, floor 1
        elevs[1].CurrentFloor = 10; // id 2, floor 10

        manager.ReceiveRequest(new HallRequest(2, Direction.Up)); // Should go to elevator 1
        manager.ReceiveRequest(new HallRequest(4, Direction.Up)); // Should batch to elevator 1
        manager.ReceiveRequest(new HallRequest(9, Direction.Down)); // Should go to elevator 2

        manager.AssignRequests();

        // Check elevator 1 handled two "Up" requests
        var elev1 = manager.GetElevators().First(e => e.Id == 1);
        var elev1Targets = elev1.TargetFloors.ToList();
        Assert.Contains(2, elev1Targets);
        Assert.Contains(4, elev1Targets);

        // Check elevator 2 handled one "Down" request
        var elev2 = manager.GetElevators().First(e => e.Id == 2);
        var elev2Targets = elev2.TargetFloors.ToList();
        Assert.Contains(9, elev2Targets);

        // Check request assignment
        var assigned = manager.GetPendingRequests().Where(r => r.Status == HallRequestStatus.Assigned).ToList();
        Assert.Equal(3, assigned.Count);
        Assert.Contains(assigned, r => r.Floor == 9 && r.AssignedElevatorId == 2);
        Assert.Contains(assigned, r => r.Floor == 2 && r.AssignedElevatorId == 1);
        Assert.Contains(assigned, r => r.Floor == 4 && r.AssignedElevatorId == 1);
    }

    //[Fact]
    //public void AssignRequests_Should_Assign2ndElevatorTo2ndRequest()
    //{
    //    // Arrange
    //    var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 2);
    //    var request1 = new HallRequest(8, Direction.Down); // First idle elevator should take this
    //    var request2 = new HallRequest(2, Direction.Down); // Second idle elevator should take this 

    //    // Act
    //    manager.ReceiveRequest(request1);
    //    manager.ReceiveRequest(request2);

    //    manager.AssignRequests();

    //    // Assert
    //    var elevators = manager.GetElevators();
    //    var assignedToRequest1 = elevators.FirstOrDefault(e => e.TargetFloors.Contains(4));

    //    Assert.NotNull(assignedToRequest1);
    //    Assert.Single(assignedToRequest1.TargetFloors);
    //    Assert.Equal(5, assignedToRequest1.TargetFloors.First());

    //    // The pending queue should still exist until the elevator is served.
    //    Assert.Equal(5, manager.GetPendingRequests().First().Floor);
    //    Assert.Equal(Direction.Down, manager.GetPendingRequests().First().Direction);
    //}

    [Fact]
    public void Step_MovesElevatorOneFloorTowardTarget()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 1);
        var request = new HallRequest(5, Direction.Up);
        manager.ReceiveRequest(request);
        manager.AssignRequests();

        var elevator = manager.GetElevators().Single();
        Assert.Equal(1, elevator.CurrentFloor); // starts at floor 1
        Assert.Contains(5, elevator.TargetFloors);

        // Act - move one step
        manager.Step();
        elevator = manager.GetElevators().Single();

        // Assert
        Assert.Equal(2, elevator.CurrentFloor); // moved up one floor
        Assert.Contains(5, elevator.TargetFloors); // still going to 5
    }

    [Fact]
    public void Step_RemovesTargetWhenArrived()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 1);
        var request = new HallRequest(2, Direction.Up);
        manager.ReceiveRequest(request);
        manager.AssignRequests();

        var elevator = manager.GetElevators().Single();
        elevator.CurrentFloor = 1;

        // Act - move one step to floor 2
        manager.Step(); // Should move from 1 to 2
        elevator = manager.GetElevators().Single();

        Assert.Equal(2, elevator.CurrentFloor);

        // Next step should "arrive" and dequeue the target floor
        manager.Step();
        elevator = manager.GetElevators().Single();
        Assert.Empty(elevator.TargetFloors);
        Assert.Null(elevator.Direction);
    }

    [Fact]
    public void Step_RemovesTargetWhenArrived_MultipleElevators()
    {
        // Arrange: 4 elevators, both at floor 1, requests for floor 2 and 3
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);
        var request1 = new HallRequest(2, Direction.Up);
        var request2 = new HallRequest(3, Direction.Up);
        manager.ReceiveRequest(request1);
        manager.ReceiveRequest(request2);
        manager.AssignRequests();

        // Simulate steps until all targets are reached
        var elevators = manager.GetElevators();

        // Keep stepping until all elevators have no targets
        int maxLoops = 10; // Prevent infinite loop in case of bug
        int loops = 0;
        while (elevators.Any(e => e.TargetFloors.Any()) && loops++ < maxLoops)
        {
            manager.Step();
            elevators = manager.GetElevators();
        }

        // Assert: Each elevator at its assigned destination and target queue empty
        foreach (var elevator in elevators)
        {
            // If elevator was assigned a floor, it should be there, and have no targets left
            if (elevator.CurrentFloor == 2 || elevator.CurrentFloor == 3)
            {
                Assert.Empty(elevator.TargetFloors);
                Assert.Null(elevator.Direction);
            }
            else
            {
                // Elevators not assigned a target should be idle on floor 1
                Assert.Equal(1, elevator.CurrentFloor);
                Assert.Empty(elevator.TargetFloors);
                Assert.Null(elevator.Direction);
            }
        }
    }
}
