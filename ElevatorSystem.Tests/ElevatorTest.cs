using ElevatorSystem.Models;
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
}
