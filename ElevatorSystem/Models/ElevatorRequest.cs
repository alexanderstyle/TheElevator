namespace ElevatorSystem.Models;

public enum Direction
{
    Up,
    Down
}

public class ElevatorRequest
{
    public int Floor { get; set; }
    public Direction Direction { get; set; }

    public ElevatorRequest(int floor, Direction direction)
    {
        Floor = floor;
        Direction = direction;
    }
}
