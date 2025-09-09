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
    public void ReceiveRequestShouldAddRequestToQueue()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var request = new HallRequest(5, Direction.Down);

        // Act
        manager.ReceiveRequest(request);
        var pending = manager.GetAllPendingRequests();

        // Assert
        Assert.Single(pending);
        Assert.Equal(5, pending[0].Floor);
        Assert.Equal(Direction.Down, pending[0].Direction);
        Assert.Equal(HallRequestStatus.Pending, pending[0].Status);
        Assert.Null(pending[0].AssignedElevatorId);
    }

    [Fact]
    public void ReceiveRequestShouldAddMultipleRequestToQueue()
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
    public void ReceiveRequestShouldAddSameFloorButDifferentDirectionToQueue()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object);
        var request = new HallRequest(5, Direction.Down);
        var request1 = new HallRequest(8, Direction.Down);
        var request2 = new HallRequest(5, Direction.Up);

        // Act
        manager.ReceiveRequest(request);
        manager.ReceiveRequest(request1);
        manager.ReceiveRequest(request2);
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
    public void ReceiveRequestShouldAddMultipleRequestsInQueueAndInOrder()
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
    public void ReceiveRequestsShouldNotAssignIdenticalRequestToElevator()
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

        var pending = manager.GetAllPendingRequests();

        // Assert
        Assert.Equal(2, pending.Count); // Only 2 should be added, not the duplicate
        Assert.Equal(5, pending[0].Floor);
        Assert.Equal(8, pending[1].Floor);
    }

    [Fact]
    public void AssignRequestsShouldBatchAllUpRequestsToSingleIdleElevatorBelow()
    {
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        // Should assign to closest (elevator 1 which is on 3rd floor)
        manager.SetElevatorCurrentFloor(1, 3);
        manager.SetElevatorCurrentFloor(2, 2);
        manager.SetElevatorCurrentFloor(3, 1);
        manager.SetElevatorCurrentFloor(4, 1);

        manager.ReceiveRequest(new HallRequest(6, Direction.Up));
        manager.ReceiveRequest(new HallRequest(7, Direction.Up));
        manager.ReceiveRequest(new HallRequest(8, Direction.Up));

        manager.AssignRequests();

        // Assert: All requests are assigned, and only to one elevator as a batch
        var assigned = manager.GetAssignedRequests();

        Assert.Equal(3, assigned.Count);

        var elevators = manager.GetElevators();

        // Elevator 1 is assigned but others do not.
        Assert.True(assigned[0].AssignedElevatorId == 1);
        Assert.True(assigned[1].AssignedElevatorId == 1);
        Assert.True(assigned[2].AssignedElevatorId == 1);
    }

    [Fact]
    public void AssignRequestsShouldBatchAllDownRequestsToSingleIdleElevatorAbove()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        // Should assign to closest (elevator 1 which is on 7th floor)
        manager.SetElevatorCurrentFloor(1, 7);
        manager.SetElevatorCurrentFloor(2, 8);
        manager.SetElevatorCurrentFloor(3, 9);
        manager.SetElevatorCurrentFloor(4, 9);

        // Re-inject or update the internal state of ElevatorManager if needed:
        // (If GetElevators() returns a deep copy, you'll want to set via direct state or update ElevatorManager constructor.)

        // Add Down requests matching floors 5, 4, 3
        manager.ReceiveRequest(new HallRequest(5, Direction.Down));
        manager.ReceiveRequest(new HallRequest(4, Direction.Down));
        manager.ReceiveRequest(new HallRequest(3, Direction.Down));

        // Act
        manager.AssignRequests();

        // Assert: Each elevator should have one request, assigned as expected
        var assigned = manager.GetAssignedRequests();
        Assert.Equal(3, assigned.Count);

        // Only one elevator should be assigned.
        var elevatorIdsUsed = assigned.Select(r => r.AssignedElevatorId).Distinct().ToList();
        Assert.Single(elevatorIdsUsed);

        // A single assigned elevator should have stops of all the requests.
        var assignedElevator = manager.GetElevators().First(e => e.Id == elevatorIdsUsed[0]);
        Assert.Equal(new List<int> { 5, 4, 3 }, assignedElevator.TargetFloors);

        Assert.Equal(Direction.Down, assignedElevator.Direction);

        // Elevator 1 is assigned but others do not.
        Assert.True(assigned[0].AssignedElevatorId == 1);
        Assert.True(assigned[1].AssignedElevatorId == 1);
        Assert.True(assigned[2].AssignedElevatorId == 1);
    }

    [Fact]
    public void AssignRequestsShouldBatchOnTheWayRequestsToGoingUpElevator()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        manager.SetElevatorState(1, 3, Direction.Up);
        manager.SetElevatorState(2, 1, Direction.Up);
        manager.SetElevatorState(3, 2, Direction.Up);
        manager.SetElevatorState(4, 2, Direction.Up);

        manager.ReceiveRequest(new HallRequest(6, Direction.Up));
        manager.ReceiveRequest(new HallRequest(7, Direction.Up));
        manager.ReceiveRequest(new HallRequest(8, Direction.Up));

        // Act
        manager.AssignRequests();

        // Stops have been defined (target floors) but on the way a new request comes in.
        manager.ReceiveRequest(new HallRequest(5, Direction.Up));

        manager.AssignRequests();

        // Assert
        // Should choose the first elevator.
        // Should also include request floor 5 (Target floors will be 5 6 7 8).
        // No more pending.
        // All requests assigned.
        Assert.Equal(1, manager.GetAssignedRequests()[0].AssignedElevatorId);
        Assert.Equal(1, manager.GetAssignedRequests()[1].AssignedElevatorId);
        Assert.Equal(1, manager.GetAssignedRequests()[2].AssignedElevatorId);
        Assert.Equal(1, manager.GetAssignedRequests()[3].AssignedElevatorId);

        Assert.Equal(manager.GetElevators()[0].TargetFloors, new List<int> { 5, 6, 7, 8 });

        Assert.Empty(manager.GetPendingUpRequests());
        Assert.Equal(4, manager.GetAssignedRequests().Count);
    }

    //[Fact]
    //public void AssignRequestsShouldBatchOnTheWayRequestsToMovingElevator()
    //{
    //    // Arrange
    //    var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

    //    // Set up Elevator 1 as already moving Up to floor 7
    //    var internalElevator = manager.GetActualElevatorById(1);
    //    internalElevator.CurrentFloor = 2;
    //    internalElevator.Direction = Direction.Up;
    //    internalElevator.TargetFloors.Add(7);

    //    // Mark as assigned the initial request to 7 and inject it directly
    //    var existingRequest = new HallRequest(7, Direction.Up)
    //    {
    //        Status = HallRequestStatus.Assigned,
    //        AssignedElevatorId = internalElevator.Id
    //    };
    //    manager.ClearAndInjectRequests(new List<HallRequest> { existingRequest });

    //    // Add Up-pending requests "on the way" (between current floor and 7)
    //    manager.ReceiveRequest(new HallRequest(3, Direction.Up)); // on the way to 7
    //    manager.ReceiveRequest(new HallRequest(6, Direction.Up)); // on the way to 7

    //    // Act
    //    manager.AssignRequests();

    //    // Assert
    //    var assigned = manager.GetAssignedRequests().Where(r => r.AssignedElevatorId == internalElevator.Id).ToList();
    //    Assert.Contains(assigned, r => r.Floor == 3);
    //    Assert.Contains(assigned, r => r.Floor == 6);
    //    Assert.Contains(assigned, r => r.Floor == 7);

    //    // Elevator 1 should have 3, 6, 7 as its targets
    //    var assignedElevator = manager.GetActualElevatorById(1);
    //    Assert.Contains(3, assignedElevator.TargetFloors);
    //    Assert.Contains(6, assignedElevator.TargetFloors);
    //    Assert.Contains(7, assignedElevator.TargetFloors);
    //    Assert.Equal(Direction.Up, assignedElevator.Direction);

    //    // All other elevators should still be idle and have no targets
    //    var otherElevators = manager.GetElevators().Where(e => e.Id != assignedElevator.Id);
    //    Assert.All(otherElevators, e =>
    //    {
    //        Assert.True(e.IsIdle);
    //        Assert.Empty(e.TargetFloors);
    //    });
    //}

    [Fact]
    public void StepMovesElevatorOneFloorTowardTarget()
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
    public void StepRemovesTargetWhenArrived()
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
    public void StepShouldRemoveTargetWhenArrivedMultipleElevators()
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

    [Fact]
    public void GetClosestIdleElevatorBelowUpRequestsShouldReturnClosestElevator()
    {
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        manager.ReceiveRequest(new HallRequest(3, Direction.Up));
        manager.ReceiveRequest(new HallRequest(4, Direction.Up));
        manager.ReceiveRequest(new HallRequest(5, Direction.Up));

        // Should return 3.
        manager.SetElevatorCurrentFloor(1, 3);
        manager.SetElevatorCurrentFloor(2, 2);
        manager.SetElevatorCurrentFloor(3, 1);
        manager.SetElevatorCurrentFloor(4, 1);

        var closest = manager.GetClosestIdleElevatorForUpRequests();

        // Assert
        Assert.NotNull(closest);
        Assert.Equal(1, closest.Id);
    }

    [Fact]
    public void GetClosestIdleElevatorBelowReorderedUpRequestsShouldReturnClosestElevator()
    {
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        manager.ReceiveRequest(new HallRequest(3, Direction.Up));
        manager.ReceiveRequest(new HallRequest(4, Direction.Up));
        manager.ReceiveRequest(new HallRequest(5, Direction.Up));

        // Should return 3.
        manager.SetElevatorCurrentFloor(1, 3);
        manager.SetElevatorCurrentFloor(2, 2);
        manager.SetElevatorCurrentFloor(3, 1);
        manager.SetElevatorCurrentFloor(4, 1);

        var closest = manager.GetClosestIdleElevatorForUpRequests();

        // Assert
        Assert.NotNull(closest);
        Assert.Equal(1, closest.Id);
    }


    [Fact]
    public void GetClosestIdleElevatorAboveDownRequestsShouldReturnClosestElevator()
    {
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        manager.ReceiveRequest(new HallRequest(3, Direction.Down));
        manager.ReceiveRequest(new HallRequest(4, Direction.Down));
        manager.ReceiveRequest(new HallRequest(5, Direction.Down));

        // Should return 7.
        manager.SetElevatorCurrentFloor(1, 7);
        manager.SetElevatorCurrentFloor(2, 8);
        manager.SetElevatorCurrentFloor(3, 9);
        manager.SetElevatorCurrentFloor(4, 9);

        var closest = manager.GetClosestIdleElevatorForDownRequests();

        // Assert
        Assert.NotNull(closest);
        Assert.Equal(1, closest.Id);
    }

    [Fact]
    public void GetClosestIdleElevatorAboveReorderedDownRequestsShouldReturnClosestElevator()
    {
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        manager.ReceiveRequest(new HallRequest(3, Direction.Down));
        manager.ReceiveRequest(new HallRequest(4, Direction.Down));
        manager.ReceiveRequest(new HallRequest(5, Direction.Down));

        // Should return 7.
        manager.SetElevatorCurrentFloor(1, 9);
        manager.SetElevatorCurrentFloor(2, 8);
        manager.SetElevatorCurrentFloor(3, 7);
        manager.SetElevatorCurrentFloor(4, 7);

        var closest = manager.GetClosestIdleElevatorForDownRequests();

        // Assert
        Assert.NotNull(closest);
        Assert.Equal(3, closest.Id);
    }
}
