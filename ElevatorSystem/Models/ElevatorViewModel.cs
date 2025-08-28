namespace ElevatorSystem.Models;

/// <summary>
/// ViewModel for displaying elevator status in the UI.
/// </summary>
public class ElevatorViewModel
{
    public int Id { get; set; }

    public int CurrentFloor { get; set; }

    public string Direction { get; set; } = string.Empty;

    public List<int> TargetFloors { get; set; } = new List<int>();

    public bool IsIdle { get; set; }

    public ElevatorViewModel(Elevator elevator)
    {
        Id = elevator.Id;
        CurrentFloor = elevator.CurrentFloor;
        Direction = elevator.Direction?.ToString() ?? "";
        TargetFloors = elevator.TargetFloors.ToList();
        IsIdle = elevator.IsIdle;
    }

    public ElevatorViewModel()
    {
    }
}