using ElevatorSystem.Controllers;

namespace ElevatorSystem.Services;

/// <summary>
/// A background service that manages the operation of elevators by periodically processing requests and moving
/// elevators. This acts as our simulation "engine" for moving elevator cars.
/// </summary>
/// <remarks>This service runs continuously in the background, invoking the <see
/// cref="ElevatorManager.AssignRequests"/> method to assign pending requests and the <see cref="ElevatorManager.Step"/>
/// method to move elevators at regular intervals. The interval between ticks is set to 10 seconds by default.</remarks>
public class ElevatorBackgroundService : BackgroundService
{
    private readonly ILogger<ElevatorBackgroundService> _logger;
    private readonly ElevatorManager _manager;

    // Default interval, in seconds our elevator operations logic will fire.
    private readonly long _defaultTickIntervalSeconds = 10;

    // Tick interval, in seconds our elevator operations logic will fire.
    private readonly TimeSpan _tickInterval;

    public ElevatorBackgroundService(ILogger<ElevatorBackgroundService> logger,
        ElevatorManager manager)
    {
        _logger = logger;
        _manager = manager;
        _tickInterval = TimeSpan.FromSeconds(_defaultTickIntervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_tickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _manager.AssignRequests();

                _manager.Step();

                _logger.LogInformation("Running scheduled task...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled task");
            }
        }
    }
}