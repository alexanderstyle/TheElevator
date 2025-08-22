namespace ElevatorControlSystem.Models;

public enum Direction
{
    Up,
    Down
}

public class ElevatorRequest
{
    public int Floor { get; set; }
    public Direction Direction { get; set; }
}
