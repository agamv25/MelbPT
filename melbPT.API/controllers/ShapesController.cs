using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace melbPT.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShapesController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        public ShapesController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpGet("{mode}")]
        public IActionResult Get(string mode)
        {
            if (_cache.TryGetValue($"GtfsShapesGeoJson:{mode}", out object shapesGeoJson))
            {
                return Ok(shapesGeoJson);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("Stops/{mode}")]
        public IActionResult GetStops(string mode)
        {
            if(_cache.TryGetValue($"GtfsStopsGeoJson:{mode}", out object stopsGeoJson))
            {
                return Ok(stopsGeoJson);
            }
            else
            {
                return NotFound();
            }
        }
    }
}