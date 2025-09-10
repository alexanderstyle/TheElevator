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

    //[Fact(Skip = "New naive implementations but reuse this")]
    //public async Task StepAsyncShouldRemoveTargetWhenArrived()
    //{
    //    // Arrange
    //    var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 1);
    //    var request = new HallRequest(2, Direction.Up);
    //    await manager.ReceiveRequestAsync(request);
    //    await manager.AssignRequestAsync();

    //    var elevator = manager.GetElevators().Single();
    //    elevator.CurrentFloor = 1;

    //    // Remove delay in onloading during tests.
    //    manager.OnloadingDelayInSeconds = 0;

    //    // Act - move one step to floor 2
    //    await manager.StepAsync(); // Should move from 1 to 2
    //    elevator = manager.GetElevators().Single();

    //    Assert.Equal(2, elevator.CurrentFloor);

    //    // Next step should "arrive" and dequeue the target floor
    //    await manager.StepAsync();
    //    elevator = manager.GetElevators().Single();
    //    Assert.Empty(elevator.TargetFloors);
    //    Assert.Null(elevator.Direction);
    //}

    //[Fact(Skip = "New naive implementations but reuse this")]
    //public async Task StepAsyncShouldRemoveTargetWhenArrivedMultipleElevators()
    //{
    //    // Arrange: 4 elevators, both at floor 1, requests for floor 2 and 3
    //    var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);
    //    var request1 = new HallRequest(2, Direction.Up);
    //    var request2 = new HallRequest(3, Direction.Up);
    //    await manager.ReceiveRequestAsync(request1);
    //    await manager.ReceiveRequestAsync(request2);
    //    await manager.AssignRequestAsync();

    //    // Simulate steps until all targets are reached
    //    var elevators = manager.GetElevators();

    //    // Remove delay in onloading during tests.
    //    manager.OnloadingDelayInSeconds = 0;

    //    // Keep stepping until all elevators have no targets
    //    int maxLoops = 10; // Prevent infinite loop in case of bug
    //    int loops = 0;
    //    while (elevators.Any(e => e.TargetFloors.Any()) && loops++ < maxLoops)
    //    {
    //        await manager.StepAsync();
    //        elevators = manager.GetElevators();
    //    }

    //    // Assert: Each elevator at its assigned destination and target queue empty
    //    foreach (var elevator in elevators)
    //    {
    //        // If elevator was assigned a floor, it should be there, and have no targets left
    //        if (elevator.CurrentFloor == 2 || elevator.CurrentFloor == 3)
    //        {
    //            Assert.Empty(elevator.TargetFloors);
    //            Assert.Null(elevator.Direction);
    //        }
    //        else
    //        {
    //            // Elevators not assigned a target should be idle on floor 1
    //            Assert.Equal(1, elevator.CurrentFloor);
    //            Assert.Empty(elevator.TargetFloors);
    //            Assert.Null(elevator.Direction);
    //        }
    //    }
    //}

    //[Fact(Skip = "New naive implementations")]
    //public async Task GetClosestIdleElevatorBelowUpRequestsShouldReturnClosestElevator()
    //{
    //    var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

    //    await manager.ReceiveRequestAsync(new HallRequest(3, Direction.Up));
    //    await manager.ReceiveRequestAsync(new HallRequest(4, Direction.Up));
    //    await manager.ReceiveRequestAsync(new HallRequest(5, Direction.Up));

    //    // Should return 3.
    //    manager.SetElevatorCurrentFloor(1, 3);
    //    manager.SetElevatorCurrentFloor(2, 2);
    //    manager.SetElevatorCurrentFloor(3, 1);
    //    manager.SetElevatorCurrentFloor(4, 1);

    //    var closest = manager.GetClosestIdleElevatorForUpRequests();

    //    // Assert
    //    Assert.NotNull(closest);
    //    Assert.Equal(1, closest.Id);
    //}

    //[Fact(Skip = "New naive implementations")]
    //public async Task GetClosestIdleElevatorBelowReorderedUpRequestsShouldReturnClosestElevator()
    //{
    //    var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

    //    await manager.ReceiveRequestAsync(new HallRequest(3, Direction.Up));
    //    await manager.ReceiveRequestAsync(new HallRequest(4, Direction.Up));
    //    await manager.ReceiveRequestAsync(new HallRequest(5, Direction.Up));

    //    // Should return 3.
    //    manager.SetElevatorCurrentFloor(1, 3);
    //    manager.SetElevatorCurrentFloor(2, 2);
    //    manager.SetElevatorCurrentFloor(3, 1);
    //    manager.SetElevatorCurrentFloor(4, 1);

    //    var closest = manager.GetClosestIdleElevatorForUpRequests();

    //    // Assert
    //    Assert.NotNull(closest);
    //    Assert.Equal(1, closest.Id);
    //}

    //[Fact(Skip = "New naive implementations")]
    //public async Task GetClosestIdleElevatorAboveDownRequestsShouldReturnClosestElevator()
    //{
    //    var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

    //    await manager.ReceiveRequestAsync(new HallRequest(3, Direction.Down));
    //    await manager.ReceiveRequestAsync(new HallRequest(4, Direction.Down));
    //    await manager.ReceiveRequestAsync(new HallRequest(5, Direction.Down));

    //    // Should return 7.
    //    manager.SetElevatorCurrentFloor(1, 7);
    //    manager.SetElevatorCurrentFloor(2, 8);
    //    manager.SetElevatorCurrentFloor(3, 9);
    //    manager.SetElevatorCurrentFloor(4, 9);

    //    var closest = manager.GetClosestIdleElevatorForDownRequests();

    //    // Assert
    //    Assert.NotNull(closest);
    //    Assert.Equal(1, closest.Id);
    //}

    //[Fact(Skip = "New naive implementations")]
    //public async Task GetClosestIdleElevatorAboveReorderedDownRequestsShouldReturnClosestElevator()
    //{
    //    var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

    //    await manager.ReceiveRequestAsync(new HallRequest(3, Direction.Down));
    //    await manager.ReceiveRequestAsync(new HallRequest(4, Direction.Down));
    //    await manager.ReceiveRequestAsync(new HallRequest(5, Direction.Down));

    //    // Should return 7.
    //    manager.SetElevatorCurrentFloor(1, 9);
    //    manager.SetElevatorCurrentFloor(2, 8);
    //    manager.SetElevatorCurrentFloor(3, 7);
    //    manager.SetElevatorCurrentFloor(4, 7);

    //    var closest = manager.GetClosestIdleElevatorForDownRequests();

    //    // Assert
    //    Assert.NotNull(closest);
    //    Assert.Equal(3, closest.Id);
    //}

    //[Fact]
    //public async Task GetClosestIdleElevatorForDownRequestShouldAssignIdleDownElevator()
    //{
    //    var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

    //}
}
