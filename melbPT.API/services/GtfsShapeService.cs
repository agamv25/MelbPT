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

                if (shapesEntry != null)
                {
                    using var entryStream = shapesEntry.Open();
                    using var reader = new StreamReader(entryStream);
                    var content = await reader.ReadToEndAsync(stoppingToken);
                    _cache.Set("GtfsShapes", content, TimeSpan.FromHours(1));
                    var lines = content.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
                    var shapePoints = lines.Select(line =>
                    {
                        var columns = line.Split(',');
                        var shapeId = columns[0].Trim('"');
                        var latitude = double.Parse(columns[1].Trim('"'));
                        var longitude = double.Parse(columns[2].Trim('"'));
                        var sequence = int.Parse(columns[3].Trim('"'));
                        return (ShapeId: shapeId, Latitude: latitude, Longitude: longitude, Sequence: sequence);
                    }).ToList();
                    var features = shapePoints
                        .GroupBy(p => p.ShapeId)
                        .Select(group => new
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
                                shape_id = group.Key
                            }
                        }).ToList();
                        
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