using Microsoft.Extensions.Caching.Memory;
namespace melbPT.API.Services
{
    public class GtfsPollingService : BackgroundService
    {
        private readonly ILogger<GtfsPollingService> _logger;
        private readonly IMemoryCache _cache;
        public GtfsPollingService(ILogger<GtfsPollingService> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
