using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Caching.Memory;
namespace melbPT.API.Services
{
    public class GtfsShapeService : BackgroundService
    {
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GtfsShapeService> _logger;

        public GtfsShapeService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<GtfsShapeService> logger)
        {
            this._httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("https://data.ptv.vic.gov.au/downloads/gtfs.zip");
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(stoppingToken);
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
                var innerZipEntry = zip.GetEntry("2/google_transit.zip");
                using var innerZipStream = innerZipEntry!.Open();
                using var innerZip = new ZipArchive(innerZipStream, ZipArchiveMode.Read);
                var shapesEntry = innerZip.GetEntry("shapes.txt");
                var tripsEntry = innerZip.GetEntry("trips.txt");
                var routesEntry = innerZip.GetEntry("routes.txt");

                if (shapesEntry != null && tripsEntry != null && routesEntry != null)
                {
                    using var entryStream = shapesEntry.Open();
                    using var reader = new StreamReader(entryStream);
                    var content = await reader.ReadToEndAsync(stoppingToken);
                    _cache.Set("GtfsShapes", content, TimeSpan.FromHours(1));
                    var lines = content.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

                    using var tripsStream = tripsEntry!.Open();
                    using var tripsReader = new StreamReader(tripsStream);
                    var tripsContent = await tripsReader.ReadToEndAsync(stoppingToken);
                    _cache.Set("GtfsTrips", tripsContent, TimeSpan.FromHours(1));

                    using var routesStream = routesEntry!.Open();
                    using var routesReader = new StreamReader(routesStream);
                    var routesContent = await routesReader.ReadToEndAsync(stoppingToken);
                    _cache.Set("GtfsRoutes", routesContent, TimeSpan.FromHours(1));

                    var shapePoints = lines.Select(line =>
                    {
                        var columns = line.Split(',');
                        var shapeId = columns[0].Trim('"');
                        var latitude = double.Parse(columns[1].Trim('"'));
                        var longitude = double.Parse(columns[2].Trim('"'));
                        var sequence = int.Parse(columns[3].Trim('"'));
                        return (ShapeId: shapeId, Latitude: latitude, Longitude: longitude, Sequence: sequence);
                    }).ToList();

                    var tripLines = tripsContent.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
                    var shapeToRoute = tripLines.Select(line =>
                    {
                        var columns = line.Split(',');
                        var shapeId = columns[3].Trim('"');
                        var routeId = columns[0].Trim('"');
                        return (ShapeId: shapeId, RouteId: routeId);
                    }).ToList();

                    var routeLines = routesContent.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
                    var routeColors = routeLines.Select(line =>
                    {
                        var columns = line.Split(',');
                        var routeId = columns[0].Trim('"');
                        var routeColor = columns[5].Trim('"');
                        return (RouteId: routeId, RouteColor: routeColor);
                    }).ToList();

                    var features = shapePoints
                        .GroupBy(p => p.ShapeId)
                        .Select(group =>
                        {
                            var routeId = shapeToRoute.FirstOrDefault(x => x.ShapeId == group.Key).RouteId;
                            var routeColor = routeColors.FirstOrDefault(r => r.RouteId == routeId).RouteColor;

                            return new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "LineString",
                                    coordinates = group.OrderBy(p => p.Sequence)
                                                       .Select(p => new[] { p.Longitude, p.Latitude })
                                                       .ToArray()
                                },
                                properties = new
                                {
                                    shape_id = group.Key,
                                    route_id = routeId,
                                    route_color = routeColor
                                }
                            };
                        }).Where(feature => !feature.properties.route_id.EndsWith("-R:")).ToList();
                    var invalidShapes = shapePoints
                        .GroupBy(p => p.ShapeId)
                        .Where(group => group.Count() < 2)
                        .Select(group => group.Key)
                        .ToList();

                    _logger.LogWarning($"Found {invalidShapes.Count} shapes with fewer than 2 points: {string.Join(", ", invalidShapes)}");
                    var featureCollection = new
                    {
                        type = "FeatureCollection",
                        features = features
                    };
                    _cache.Set("GtfsShapesGeoJson", featureCollection, TimeSpan.FromHours(1));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GTFS shapes");
            }
        }
    }
}