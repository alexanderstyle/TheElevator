namespace ElevatorSystem.Models;

public class HallRequest
{
    public int Floor { get; set; }
    public Direction Direction { get; set; }
    public HallRequestStatus Status { get; set; } = HallRequestStatus.Pending;
    public int? AssignedElevatorId { get; set; }

    public HallRequest()
    {

    }

    public HallRequest(int Floor, Direction Direction) 
    { 
        this.Floor = Floor; 
        this.Direction = Direction; 
    }
}
