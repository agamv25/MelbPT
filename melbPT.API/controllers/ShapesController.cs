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

        [HttpGet]
        public IActionResult Get()
        {
            if (_cache.TryGetValue("GtfsShapesGeoJson", out object shapesGeoJson))
            {
                return Ok(shapesGeoJson);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("trips")]
        public IActionResult GetTrips()
        {
            if (_cache.TryGetValue("GtfsTrips", out object trips))
            {
                return Ok(trips);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("routes")]
        public IActionResult GetRoutes()
        {
            if (_cache.TryGetValue("GtfsRoutes", out object routes))
            {
                return Ok(routes);
            }
            else
            {
                return NotFound();
            }
        }
    }
}