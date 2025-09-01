namespace ElevatorSystem.Models;

public class Elevator
{
    public int Id { get; }
    public int CurrentFloor { get; set; }
    public Direction? Direction { get; set; }
    public List<int> TargetFloors { get; } = new();
    public bool IsIdle => TargetFloors.Count == 0;

    public Elevator(int id, int startingFloor = 1)
    {
        Id = id;
        CurrentFloor = startingFloor;
    }

    public Elevator(Elevator other)
    {
        Id = other.Id;
        CurrentFloor = other.CurrentFloor;
        Direction = other.Direction;
        TargetFloors = new List<int>(other.TargetFloors);
    }
}
