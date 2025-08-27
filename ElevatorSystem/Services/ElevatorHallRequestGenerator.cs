using ElevatorSystem.Models;
using Microsoft.Extensions.Logging;

namespace ElevatorSystem.Services;

public class ElevatorHallRequestGenerator : BackgroundService
{
    private readonly ILogger<ElevatorHallRequestGenerator> _logger;
    private readonly ElevatorManager _manager;
    private readonly Random randomizer = new();
    private readonly int _floorCount = 10;

    public ElevatorHallRequestGenerator(ILogger<ElevatorHallRequestGenerator> logger,
        ElevatorManager manager)
    {
        _logger = logger;
        _manager = manager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            int floor = randomizer.Next(1, _floorCount + 1);

            // Pick a random direction for that floor (must be valid).
            Direction direction;
            if (floor == 1)
                direction = Direction.Up;
            else if (floor == _floorCount)
                direction = Direction.Down;
            else
                direction = randomizer.Next(0, 2) == 0 ? Direction.Up : Direction.Down;

            // Create and submit the request
            var request = new ElevatorRequest(floor, direction);
            _manager.ReceiveRequest(request);

            _logger.LogInformation($"[TIMER] Auto: \"{direction}\" request on floor {floor} received");
        }
    }
}
