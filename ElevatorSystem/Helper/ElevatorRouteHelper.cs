using ElevatorSystem.Models;

namespace ElevatorSystem.Helper;

public static class ElevatorRouteHelper
{
    /// <summary>
    /// Insert a floor into the elevator's target list in proper direction order (no duplicates).
    /// Ensures the elevator serves new enroute ("on the way") requests in the right direction and order, 
    /// enabling real-world batching and minimizing wasted movement
    /// </summary>
    public static void InsertTargetFloorInDirectionOrder(Elevator elevator, int floor)
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
}