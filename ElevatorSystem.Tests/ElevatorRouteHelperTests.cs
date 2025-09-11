using ElevatorSystem.Helper;
using ElevatorSystem.Models;
using Xunit;

namespace ElevatorSystem.Tests;

public class ElevatorRouteHelperTests
{
    [Fact]
    public void InsertTargetFloorNullDirectionShouldAddFloorNoSort()
    {
        var elevator = new Elevator(1) { Direction = null };
        elevator.TargetFloors.AddRange(new[] { 2, 1 });

        ElevatorRouteHelper.InsertTargetFloorInDirectionOrder(elevator, 3);

        // Should just append 3 at the end: [2, 1, 3]
        Assert.Equal(new List<int> { 2, 1, 3 }, elevator.TargetFloors);
    }

    [Fact]
    public void InsertTargetFloorUpDirectionShouldSortsAscendingNoDuplicates()
    {
        var elevator = new Elevator(1) { Direction = Direction.Up };
        elevator.TargetFloors.AddRange(new[] { 3, 7 });

        ElevatorRouteHelper.InsertTargetFloorInDirectionOrder(elevator, 5);

        Assert.Equal(new List<int> { 3, 5, 7 }, elevator.TargetFloors);
    }

    [Fact]
    public void InsertTargetFloorDownDirectionShouldSortsDescendingNoDuplicates()
    {
        var elevator = new Elevator(2) { Direction = Direction.Down };
        elevator.TargetFloors.AddRange(new[] { 7, 3 });

        ElevatorRouteHelper.InsertTargetFloorInDirectionOrder(elevator, 5);

        Assert.Equal(new List<int> { 7, 5, 3 }, elevator.TargetFloors);
    }

    [Fact]
    public void InsertTargetFloorDuplicateExpectNoChange()
    {
        var elevator = new Elevator(3) { Direction = null };
        elevator.TargetFloors.AddRange(new[] { 2, 1 });

        ElevatorRouteHelper.InsertTargetFloorInDirectionOrder(elevator, 2);

        Assert.Equal(new List<int> { 2, 1 }, elevator.TargetFloors);
    }
}