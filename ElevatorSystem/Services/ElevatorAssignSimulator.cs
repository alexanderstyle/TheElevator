using ElevatorSystem.Controllers;
using ElevatorSystem.Models;

namespace ElevatorSystem.Services;

/// <summary>
/// A background service that manages the operation of elevators by periodically processing requests and moving
/// elevators. This acts as our simulation "engine" for moving elevator cars.
/// </summary>
/// <remarks>This service runs continuously in the background, invoking the <see
/// cref="ElevatorManager.AssignRequests"/> method to assign pending requests and the <see cref="ElevatorManager.Step"/>
/// method to move elevators at regular intervals. The interval between ticks is set to 10 seconds by default.</remarks>
public class ElevatorAssignSimulator : BackgroundService
{
    private readonly ILogger<ElevatorAssignSimulator> _logger;
    private readonly ElevatorManager _manager;

    // Default interval, in seconds our elevator operations logic will fire.
    private readonly long _defaultTickIntervalSeconds = 5;

    private readonly Random randomizer = new Random();

    public ElevatorAssignSimulator(ILogger<ElevatorAssignSimulator> logger,
        ElevatorManager manager)
    {
        _logger = logger;
        _manager = manager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var elevatorTimer = new PeriodicTimer(TimeSpan.FromSeconds(_defaultTickIntervalSeconds));

        while (await elevatorTimer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _manager.AssignRequests();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled task");
            }
        }
    }
}