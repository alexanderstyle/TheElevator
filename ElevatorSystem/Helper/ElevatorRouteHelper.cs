using ElevatorSystem.Models;

namespace ElevatorSystem.Helper;

public static class ElevatorRouteHelper
{
    /// <summary>
    /// Insert a floor into the elevator's target list in proper direction order (no duplicates).
    /// </summary>
    public static void InsertFloorInDirectionOrder(Elevator elevator, int floor)
    {
        if (!elevator.TargetFloors.Contains(floor))
        {
            elevator.TargetFloors.Add(floor);

            if (elevator.Direction == Direction.Up)
            {
                // Ascending
                elevator.TargetFloors.Sort();
            }
            else if (elevator.Direction == Direction.Down)
            {
                // Descending
                elevator.TargetFloors.Sort((a, b) => b.CompareTo(a));
            }

            // If idle: do not sort, just append.
        }
    }

    /// <summary>
    /// Returns true if the elevator is going to pass at (or stop at) requestFloor
    /// in its current direction (Up: passes floors above; Down: passes floors below).
    /// </summary>
    public static bool IsPassingFloor(Elevator elevator, int requestFloor)
    {
        if (elevator.Direction == null)
        {
            return false;
        }

        if (elevator.Direction == Direction.Up)
        {
            int highest = elevator.TargetFloors.Count > 0
                ? Math.Max(elevator.CurrentFloor, elevator.TargetFloors.Max())
                : elevator.CurrentFloor;

            return requestFloor > elevator.CurrentFloor && requestFloor <= highest;
        }
        else // Direction.Down
        {
            int lowest = elevator.TargetFloors.Count > 0
                ? Math.Min(elevator.CurrentFloor, elevator.TargetFloors.Min())
                : elevator.CurrentFloor;

            return requestFloor < elevator.CurrentFloor && requestFloor >= lowest;
        }
    }
}