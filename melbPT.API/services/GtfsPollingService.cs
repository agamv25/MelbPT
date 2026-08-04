using Microsoft.Extensions.Caching.Memory;
namespace melbPT.API.Services
{
    public class GtfsPollingService : BackgroundService
    {
        private readonly ILogger<GtfsPollingService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public GtfsPollingService(ILogger<GtfsPollingService> logger, IMemoryCache cache, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var metroTask = FetchAndCacheAsync("https://api.opendata.transport.vic.gov.au/opendata/public-transport/gtfs/realtime/v1/metro/vehicle-positions", "GtfsVehiclePositions:metro", TimeSpan.FromSeconds(30), stoppingToken);
            var tramTask = FetchAndCacheAsync("https://api.opendata.transport.vic.gov.au/opendata/public-transport/gtfs/realtime/v1/tram/vehicle-positions", "GtfsVehiclePositions:tram", TimeSpan.FromSeconds(30), stoppingToken);
            var busTask = FetchAndCacheAsync("https://api.opendata.transport.vic.gov.au/opendata/public-transport/gtfs/realtime/v1/bus/vehicle-positions", "GtfsVehiclePositions:bus", TimeSpan.FromSeconds(30), stoppingToken);

            await Task.WhenAll(metroTask, tramTask, busTask);
        }
        private async Task FetchAndCacheAsync(string url, string cacheKey, TimeSpan cacheDuration, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var apiKey = _configuration.GetValue<string>("Gtfs:ApiKey");
                    var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Add("KeyId", apiKey);
                    var response = await client.GetAsync(url, stoppingToken);
                    _cache.Set(cacheKey, await response.Content.ReadAsByteArrayAsync(), cacheDuration);
                    await Task.Delay(cacheDuration, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error fetching and caching data from {url}");
                }
            }
        }
    }
}
