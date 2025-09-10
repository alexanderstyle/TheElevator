namespace ElevatorSystem.Models;

public class HallRequestViewModel
{
    public int Floor { get; set; }
    public string Direction { get; set; } = string.Empty;

    public HallRequestViewModel() { }

    public HallRequestViewModel(HallRequest request)
    {
        Floor = request.Floor;
        Direction = request.Direction.ToString();
    }
}