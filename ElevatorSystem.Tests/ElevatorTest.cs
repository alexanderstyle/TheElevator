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
    public void ReceiveRequest_Should_AddSameFloorButDifferentDirectionToQueue()
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
        var pending = manager.GetPendingRequests();

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
    public void AssignRequests_Should_assign_1_request_to_1st_elevator()
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
        Assert.Equal(4, elevators.Count);
        Assert.NotNull(assignedElevator);
        Assert.Equal(5, assignedElevator.TargetFloors.First());

        // No more pending requests, but there should be assigned.
        Assert.Empty(manager.GetPendingRequests());
        Assert.Single(manager.GetAssignedRequests());
    }

    [Fact]
    public void AssignRequests_Should_assign_6_request_to_elevator_1()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 1);
        var request = new HallRequest(2, Direction.Up);
        var request1 = new HallRequest(3, Direction.Up);
        var request2 = new HallRequest(4, Direction.Up);

        // Act
        manager.ReceiveRequest(request);
        manager.AssignRequests();

        // Now receive more requests and assign to that elevator as one batch.
        manager.ReceiveRequest(new HallRequest(5, Direction.Up));
        manager.ReceiveRequest(new HallRequest(6, Direction.Up));
        manager.ReceiveRequest(new HallRequest(7, Direction.Up));

        manager.AssignRequests();

        var assigned = manager.GetAssignedRequests();

        // Assert
        // Are all assigned to elevator 1?
        Assert.All(assigned, r => Assert.Equal(1, r.AssignedElevatorId));
    }

    [Fact]
    public void AssignRequests_Should_assign_first_1_request_and_next_3_requests_to_elevator_1_that_is_enroute()
    {
        // Arrange
        var manager = new ElevatorManager(_mockLogger.Object, floors: 10, elevatorCount: 4);

        // Act
        manager.ReceiveRequest(new HallRequest(6, Direction.Down));
        manager.AssignRequests();
        manager.Step(); // Move elevator 1 to floor 2.

        // More requests while elevator 1 is enroute.
        manager.ReceiveRequest(new HallRequest(4, Direction.Down));
        manager.ReceiveRequest(new HallRequest(5, Direction.Down));
        manager.AssignRequests();
        manager.Step(); // 3rd floor
        manager.Step(); // 4th floor
        manager.Step(); // 5th floor

        var elevators = manager.GetElevators();

        Assert.Equal(4, elevators.Single(e => e.Id == 1).CurrentFloor);
    }

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
