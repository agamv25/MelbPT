using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TransitRealtime;

namespace melbPT.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        public VehiclesController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Get()
        {
            if (_cache.TryGetValue("GtfsVehiclePositions", out byte[] bytes))
            {
                var feed = FeedMessage.Parser.ParseFrom(bytes);
                return Ok(feed);
            }
            else
            {
                return NotFound();
            }
        }
    }
}