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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var apiKey = _configuration.GetValue<string>("Gtfs:ApiKey");
                    var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Add("KeyId", apiKey);
                    var response = await client.GetAsync("https://api.opendata.transport.vic.gov.au/opendata/public-transport/gtfs/realtime/v1/metro/vehicle-positions", stoppingToken);
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    _cache.Set("GtfsVehiclePositions", bytes, TimeSpan.FromSeconds(30));
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling GTFS data");
                }
            }
        }
    }
}
