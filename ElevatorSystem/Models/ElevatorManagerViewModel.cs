namespace ElevatorSystem.Models;

public class ElevatorManagerViewModel
{
    public int Floors { get; set; }
    public int ElevatorCount { get; set; }
    public List<ElevatorViewModel> Elevators { get; set; }
    public List<ElevatorRequestViewModel> PendingRequests { get; set; }
}