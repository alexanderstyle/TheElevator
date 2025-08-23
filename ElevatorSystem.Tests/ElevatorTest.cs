using ElevatorSystem.Models;
using ElevatorSystem.Services;
using Xunit;

namespace ElevatorSystem.Tests;

public class ElevatorTest
{
    [Fact]
    public void ReceiveRequest_AddsRequestToQueue()
    {
        // Arrange
        var manager = new ElevatorManager();
        var request = new ElevatorRequest(5, Direction.Up);

        // Act
        manager.ReceiveRequest(request);
        var pending = manager.GetPendingRequests();

        // Assert
        Assert.Single(pending);
        Assert.Equal(5, pending[0].Floor);
        Assert.Equal(Direction.Up, pending[0].Direction);
    }

    [Fact]
    public void ReceiveRequest_MultipleRequestsAreQueuedInOrder()
    {
        // Arrange
        var manager = new ElevatorManager();
        var req1 = new ElevatorRequest(2, Direction.Up);
        var req2 = new ElevatorRequest(7, Direction.Down);

        // Act
        manager.ReceiveRequest(req1);
        manager.ReceiveRequest(req2);

        var pending = manager.GetPendingRequests();

        // Assert
        Assert.Equal(2, pending.Count);
        Assert.Equal(2, pending[0].Floor);        // FIFO order check
        Assert.Equal(7, pending[1].Floor);
    }

    [Fact]
    public void AssignRequests_AssignsRequestToElevatorAndClearsPending()
    {
        // Arrange
        var manager = new ElevatorManager(floors: 10, elevatorCount: 2);
        var request = new ElevatorRequest(5, Direction.Up);

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
        var manager = new ElevatorManager(floors: 10, elevatorCount: 1);
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
        var manager = new ElevatorManager(floors: 10, elevatorCount: 1);
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
}
