namespace ElevatorSystem.Models;

public class Elevator
{
    public int Id { get; }
    public int CurrentFloor { get; set; } = 1;
    public Direction? Direction { get; set; }
    public Queue<int> TargetFloors { get; } = new Queue<int>();

    public bool IsIdle => TargetFloors.Count == 0;

    public Elevator(int id)
    {
        Id = id;
    }

    public Elevator(Elevator other)
    {
        Id = other.Id;
        CurrentFloor = other.CurrentFloor;
        Direction = other.Direction;
        TargetFloors = new Queue<int>(other.TargetFloors); // Creates a new queue with same contents.
    }
}
