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
    public void ReceiveRequest_AddsRequestToQueue()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var request = new ElevatorRequest(5, Direction.Down);

        // Act
        manager.ReceiveRequest(request);
        var pending = manager.GetPendingRequests();

        // Assert
        Assert.Single(pending);
        Assert.Equal(5, pending[0].Floor);
        Assert.Equal(Direction.Down, pending[0].Direction);
    }

    [Fact]
    public void ReceiveRequest_AddsMultitpleRequestToQueue()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var request = new ElevatorRequest(5, Direction.Down);
        var request1 = new ElevatorRequest(8, Direction.Down);
        var request2 = new ElevatorRequest(2, Direction.Up);

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
    }

    [Fact]
    public void ReceiveRequest_MultipleRequestsAreQueuedInOrder()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var req1 = new ElevatorRequest(7, Direction.Down);
        var req2 = new ElevatorRequest(2, Direction.Up);

        // Act
        manager.ReceiveRequest(req1);
        manager.ReceiveRequest(req2);

        var pending = manager.GetPendingRequests();

        // Assert
        Assert.Equal(2, pending.Count);
        Assert.Equal(7, pending[0].Floor);        // FIFO order check
        Assert.Equal(2, pending[1].Floor);
        Assert.Equal(Direction.Down, pending[0].Direction);
        Assert.Equal(Direction.Up, pending[1].Direction);
    }

    [Fact]
    public void AssignRequests_AssignsRequestToElevatorAndClearsPending()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);
        var request = new ElevatorRequest(5, Direction.Down);

        // Act
        manager.ReceiveRequest(request);
        manager.AssignRequests();

        // Assert
        var elevators = manager.GetElevators();
        var assignedElevator = elevators.FirstOrDefault(e => e.TargetFloors.Contains(5));

        Assert.NotNull(assignedElevator);
        Assert.Single(assignedElevator.TargetFloors);
        Assert.Equal(5, assignedElevator.TargetFloors.First());

        // The pending queue should be empty after assignment
        Assert.Empty(manager.GetPendingRequests());
    }

    [Fact]
    public void Step_MovesElevatorOneFloorTowardTarget()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 1);
        var request = new ElevatorRequest(5, Direction.Up);
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
        var request = new ElevatorRequest(2, Direction.Up);
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
        var request1 = new ElevatorRequest(2, Direction.Up);
        var request2 = new ElevatorRequest(3, Direction.Up);
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
