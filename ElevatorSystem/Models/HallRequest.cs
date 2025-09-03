namespace ElevatorSystem.Models;

public class HallRequest
{
    public int Floor { get; set; }
    public Direction Direction { get; set; }
    public HallRequestStatus Status { get; set; } = HallRequestStatus.Pending;
    public int? AssignedElevatorId { get; set; }
    public HallRequest(int floor, Direction dir) 
    { 
        Floor = floor; 
        Direction = dir; 
    }
}
