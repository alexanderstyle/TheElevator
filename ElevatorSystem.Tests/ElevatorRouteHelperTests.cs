using ElevatorSystem.Helper;
using ElevatorSystem.Models;
using Xunit;

namespace ElevatorSystem.Tests;

public class ElevatorRouteHelperTests
{
    [Fact]
    public void IsPassingFloor_UpDirection_PassesFloorsAboveUpToHighestTarget()
    {
        var elevator = new Elevator(1)
        {
            CurrentFloor = 2,
            Direction = Direction.Up
        };
        elevator.TargetFloors.AddRange(new[] { 5, 8 });

        // Should pass 3, 4, 5, 6, 7, 8
        Assert.True(ElevatorRouteHelper.IsPassingFloor(elevator, 5));
        Assert.True(ElevatorRouteHelper.IsPassingFloor(elevator, 8));
        Assert.True(ElevatorRouteHelper.IsPassingFloor(elevator, 3));
        Assert.False(ElevatorRouteHelper.IsPassingFloor(elevator, 2)); // At current floor
        Assert.False(ElevatorRouteHelper.IsPassingFloor(elevator, 9)); // Above highest
        Assert.False(ElevatorRouteHelper.IsPassingFloor(elevator, 1)); // Below current
    }

    [Fact]
    public void IsPassingFloor_DownDirection_PassesFloorsBelowDownToLowestTarget()
    {
        var elevator = new Elevator(2)
        {
            CurrentFloor = 8,
            Direction = Direction.Down
        };
        elevator.TargetFloors.AddRange(new[] { 3, 6 });

        // Should pass 7, 6, 5, 4, 3
        Assert.True(ElevatorRouteHelper.IsPassingFloor(elevator, 6));
        Assert.True(ElevatorRouteHelper.IsPassingFloor(elevator, 3));
        Assert.True(ElevatorRouteHelper.IsPassingFloor(elevator, 7));
        Assert.False(ElevatorRouteHelper.IsPassingFloor(elevator, 8)); // At current floor
        Assert.False(ElevatorRouteHelper.IsPassingFloor(elevator, 9)); // Above current
        Assert.False(ElevatorRouteHelper.IsPassingFloor(elevator, 2)); // Below lowest
    }

    [Fact]
    public void IsPassingFloor_Idle_ReturnsFalse()
    {
        var elevator = new Elevator(3)
        {
            CurrentFloor = 5,
            Direction = null
        };
        Assert.False(ElevatorRouteHelper.IsPassingFloor(elevator, 4));
        Assert.False(ElevatorRouteHelper.IsPassingFloor(elevator, 6));
    }
    [Fact]
    public void InsertTargetFloor_NullDirection_SimplyAdds_NoSort()
    {
        var elevator = new Elevator(1) { Direction = null };
        elevator.TargetFloors.AddRange(new[] { 2, 1 });

        ElevatorRouteHelper.InsertTargetFloorInDirectionOrder(elevator, 3);

        // Should just append 3 at the end: [2, 1, 3]
        Assert.Equal(new List<int> { 2, 1, 3 }, elevator.TargetFloors);
    }

    [Fact]
    public void InsertTargetFloor_UpDirection_SortsAscending_NoDuplicates()
    {
        var elevator = new Elevator(1) { Direction = Direction.Up };
        elevator.TargetFloors.AddRange(new[] { 3, 7 });

        ElevatorRouteHelper.InsertTargetFloorInDirectionOrder(elevator, 5);

        Assert.Equal(new List<int> { 3, 5, 7 }, elevator.TargetFloors);
    }

    [Fact]
    public void InsertTargetFloor_DownDirection_SortsDescending_NoDuplicates()
    {
        var elevator = new Elevator(2) { Direction = Direction.Down };
        elevator.TargetFloors.AddRange(new[] { 7, 3 });

        ElevatorRouteHelper.InsertTargetFloorInDirectionOrder(elevator, 5);

        Assert.Equal(new List<int> { 7, 5, 3 }, elevator.TargetFloors);
    }

    [Fact]
    public void InsertTargetFloor_Duplicate_NoChange()
    {
        var elevator = new Elevator(3) { Direction = null };
        elevator.TargetFloors.AddRange(new[] { 2, 1 });

        ElevatorRouteHelper.InsertTargetFloorInDirectionOrder(elevator, 2);

        Assert.Equal(new List<int> { 2, 1 }, elevator.TargetFloors);
    }
}