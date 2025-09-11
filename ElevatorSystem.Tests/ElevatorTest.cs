using Castle.Core.Logging;
using ElevatorSystem.Models;
using ElevatorSystem.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;
using Xunit;

namespace ElevatorSystem.Tests;

public class ElevatorTest
{
    private Mock<ILogger<ElevatorManager>> _mockLogger = new Mock<ILogger<ElevatorManager>>();

    [Fact]
    public async Task ReceiveRequestAsyncShouldAddRequestToQueue()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var request = new HallRequest(5, Direction.Down);

        // Act
        await manager.ReceiveRequestAsync(request);
        var pending = manager.GetAllPendingRequests();

        // Assert
        Assert.Single(pending);
        Assert.Equal(5, pending[0].Floor);
        Assert.Equal(Direction.Down, pending[0].Direction);
        Assert.Equal(HallRequestStatus.Pending, pending[0].Status);
        Assert.Null(pending[0].AssignedElevatorId);
    }

    [Fact]
    public async Task ReceiveRequestAsyncShouldAddMultipleRequestToQueue()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var request = new HallRequest(5, Direction.Down);
        var request1 = new HallRequest(8, Direction.Down);
        var request2 = new HallRequest(2, Direction.Up);

        // Act
        await manager.ReceiveRequestAsync(request);
        await manager.ReceiveRequestAsync(request1);
        await manager.ReceiveRequestAsync(request2);
        var pending = manager.GetAllPendingRequests();

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
        Assert.All(pending, r => Assert.Null(r.AssignedElevatorId));
    }

    [Fact]
    public async Task ReceiveRequestAsyncShouldAddSameFloorButDifferentDirectionToQueue()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var request = new HallRequest(5, Direction.Down);
        var request1 = new HallRequest(8, Direction.Down);
        var request2 = new HallRequest(5, Direction.Up);

        // Act
        await manager.ReceiveRequestAsync(request);
        await manager.ReceiveRequestAsync(request1);
        await manager.ReceiveRequestAsync(request2);
        var pending = manager.GetAllPendingRequests();

        // Assert
        Assert.Equal(3, pending.Count);
        Assert.Equal(5, pending[0].Floor);
        Assert.Equal(8, pending[1].Floor);
        Assert.Equal(5, pending[2].Floor);
        Assert.Equal(Direction.Down, pending[0].Direction);
        Assert.Equal(Direction.Down, pending[1].Direction);
        Assert.Equal(Direction.Up, pending[2].Direction);

        // All statuses should be Pending.
        Assert.All(pending, r => Assert.Equal(HallRequestStatus.Pending, r.Status));
    }

    [Fact]
    public async Task ReceiveRequestAsyncShouldAddMultipleRequestsInQueueAndInOrder()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var req1 = new HallRequest(7, Direction.Down);
        var req2 = new HallRequest(2, Direction.Up);
        var req3 = new HallRequest(4, Direction.Down);

        // Act
        await manager.ReceiveRequestAsync(req1);
        await manager.ReceiveRequestAsync(req2);
        await manager.ReceiveRequestAsync(req3);

        var pending = manager.GetAllPendingRequests();

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
    public async Task ReceiveRequestAsyncShouldNotAssignIdenticalRequestToElevator()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);
        var request = new HallRequest(5, Direction.Down);
        var request1 = new HallRequest(5, Direction.Down);
        var request2 = new HallRequest(8, Direction.Down);

        // Act
        await manager.ReceiveRequestAsync(request);
        await manager.ReceiveRequestAsync(request1);
        await manager.ReceiveRequestAsync(request2);

        var pending = manager.GetAllPendingRequests();

        // Assert
        Assert.Equal(2, pending.Count); // Only 2 should be added, not the duplicate
        Assert.Equal(5, pending[0].Floor);
        Assert.Equal(8, pending[1].Floor);
    }

    [Fact]
    public async Task AssignRequestAsyncShouldAssignUpRequestsToMovingUpOrIdleElevatorsBelowLowestUpRequestedFloor()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        await manager.ReceiveRequestAsync(new HallRequest(5, Direction.Up));
        await manager.ReceiveRequestAsync(new HallRequest(6, Direction.Up));
        await manager.ReceiveRequestAsync(new HallRequest(7, Direction.Up));

        // Elevators are all idle.
        manager.SetElevatorState(1, 1, null);
        manager.SetElevatorState(2, 1, null);
        manager.SetElevatorState(3, 1, Direction.Up);
        manager.SetElevatorState(4, 1, null);

        // Act
        await manager.AssignRequestAsync();

        var assigned = manager.GetAssignedRequests();

        // Assert
        Assert.Equal(3, assigned.Count);
        Assert.Equal(new List<int> { 5, 6, 7 }, manager.GetElevators().First().TargetFloors);

        // All other elevators should not have assignments.
        Assert.All(manager.GetElevators().Where(x => x.Id != 1), x => {
            Assert.Empty(x.TargetFloors);
        });
    }

    [Fact]
    public async Task AssignRequestAsyncShouldAssignDownRequestsToMovingDownOrIdleElevatorsAboveHighestDownRequestedFloor()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        await manager.ReceiveRequestAsync(new HallRequest(5, Direction.Down));
        await manager.ReceiveRequestAsync(new HallRequest(6, Direction.Down));
        await manager.ReceiveRequestAsync(new HallRequest(7, Direction.Down));

        // Elevators are all idle.
        manager.SetElevatorState(1, 8, null);
        manager.SetElevatorState(2, 9, null);
        manager.SetElevatorState(3, 10, Direction.Down);
        manager.SetElevatorState(4, 1, null);

        // Act
        await manager.AssignRequestAsync();

        var assigned = manager.GetAssignedRequests();

        // Assert
        Assert.Equal(3, assigned.Count);
        Assert.Equal(new List<int> { 7, 6, 5 }, manager.GetElevators().First().TargetFloors);

        // All other elevators should not have assignments.
        Assert.All(manager.GetElevators().Where(x => x.Id != 1), x => {
            Assert.Empty(x.TargetFloors);
        });
    }

    [Fact]
    public async Task StepAsyncShouldMoveElevatorOneFloorTowardTarget()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        // Remove delay in onloading during tests.
        manager.OnloadingDelayInSeconds = 0;

        await manager.ReceiveRequestAsync(new HallRequest(5, Direction.Up));
        await manager.ReceiveRequestAsync(new HallRequest(6, Direction.Up));
        await manager.ReceiveRequestAsync(new HallRequest(7, Direction.Up));

        manager.SetElevatorState(1, 1, null);
        manager.SetElevatorState(2, 1, null);
        manager.SetElevatorState(3, 1, null);
        manager.SetElevatorState(4, 1, null);

        await manager.AssignRequestAsync();

        // Act - move one step
        await manager.StepAsync();

        // Assert
        // First elevator should be assigned.
        Assert.Equal(1, manager.GetElevators().First().Id);
        Assert.Equal(2, manager.GetElevators().First().CurrentFloor);
        Assert.Contains(5, manager.GetElevators().First().TargetFloors); // still going to 5
        Assert.Equal(new List<int> { 5, 6, 7 }, manager.GetElevators().First().TargetFloors); // No change targets
    }

    [Fact]
    public async Task StepAsyncShouldMoveElevatorMoreFloorsTowardTarget()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        // Remove delay in onloading during tests.
        manager.OnloadingDelayInSeconds = 0;

        await manager.ReceiveRequestAsync(new HallRequest(5, Direction.Up));
        await manager.ReceiveRequestAsync(new HallRequest(6, Direction.Up));
        await manager.ReceiveRequestAsync(new HallRequest(7, Direction.Up));

        manager.SetElevatorState(1, 1, null);
        manager.SetElevatorState(2, 1, null);
        manager.SetElevatorState(3, 1, null);
        manager.SetElevatorState(4, 1, null);

        await manager.AssignRequestAsync();

        // Act 
        await manager.StepAsync(); // At floor 1 before at floor 2 after step.
        await manager.StepAsync();
        await manager.StepAsync();
        await manager.StepAsync();
        await manager.StepAsync();

        // Assert
        // First elevator should be assigned.
        Assert.Equal(1, manager.GetElevators().First().Id);
        Assert.Equal(5, manager.GetElevators().First().CurrentFloor);
        Assert.Contains(6, manager.GetElevators().First().TargetFloors); // still going to 5
        Assert.Equal(new List<int> { 6, 7 }, manager.GetElevators().First().TargetFloors); // No change targets
    }
}
