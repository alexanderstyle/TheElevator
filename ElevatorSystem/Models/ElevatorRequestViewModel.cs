namespace ElevatorSystem.Models;

public class ElevatorRequestViewModel
{
    public int Floor { get; set; }
    public string Direction { get; set; } = string.Empty;

    public ElevatorRequestViewModel() { }

    public ElevatorRequestViewModel(HallRequest request)
    {
        Floor = request.Floor;
        Direction = request.Direction.ToString();
    }
}