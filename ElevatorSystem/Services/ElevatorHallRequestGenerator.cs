using ElevatorSystem.Models;
using Microsoft.Extensions.Logging;

namespace ElevatorSystem.Services;

public class ElevatorHallRequestGenerator : BackgroundService
{
    private readonly ILogger<ElevatorHallRequestGenerator> _logger;
    private readonly ElevatorManager _manager;
    private readonly Random randomizer = new();
    private readonly int _floorCount;
    private readonly int _maxRequests;
    private int _requestCount = 0;
    private readonly long _defaultTickIntervalSeconds = 15;


    public ElevatorHallRequestGenerator(ILogger<ElevatorHallRequestGenerator> logger,
        ElevatorManager manager,
        int maxRequests = 3, 
        int floorCount = 10)
    {
        _logger = logger;
        _manager = manager;
        _maxRequests = maxRequests;
        _floorCount = floorCount;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_defaultTickIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Let's limit the number of requests generated for demo purposes.
            if (_requestCount >= _maxRequests)
            {
                Console.WriteLine($"[TIMER] Reached request limit of {_maxRequests}. Hall request generation complete.");
                break;
            }

            int floor = randomizer.Next(1, _floorCount + 1);

            // Pick a random direction for that floor (must be valid).
            Direction direction;
            if (floor == 1)
                direction = Direction.Up;
            else if (floor == _floorCount)
                direction = Direction.Down;
            else
                direction = randomizer.Next(0, 2) == 0 ? Direction.Up : Direction.Down;

            _logger.LogInformation($"\"{direction}\" request on floor {floor} auto-generated. This is request {_requestCount}.");

            // Create and submit the request.
            var request = new ElevatorRequest(floor, direction);
            _manager.ReceiveRequest(request);

            _requestCount++;
        }
    }
}
