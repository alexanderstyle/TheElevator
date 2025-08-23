namespace ElevatorSystem.Services;

/// <summary>
/// A background service that manages the operation of elevators by periodically processing requests and moving
/// elevators. This acts as our simulation "engine" for moving elevator cars.
/// </summary>
/// <remarks>This service runs continuously in the background, invoking the <see
/// cref="ElevatorManager.AssignRequests"/> method to assign pending requests and the <see cref="ElevatorManager.Step"/>
/// method to move elevators at regular intervals. The interval between ticks is set to one second by default.</remarks>
public class ElevatorBackgroundService : BackgroundService
{
    private readonly ElevatorManager _manager;

    // Fire our elevator logic every nth seconds.
    private readonly TimeSpan _tickInterval = TimeSpan.FromSeconds(1);

    public ElevatorBackgroundService(ElevatorManager manager)
    {
        _manager = manager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _manager.AssignRequests();

            _manager.Step();

            await Task.Delay(_tickInterval, stoppingToken); // Wait for next tick
        }
    }
}