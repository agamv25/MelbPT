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
            if (_cache.TryGetValue("GtfsShapes", out string shapes))
            {
                return Ok(shapes);
            }
            else
            {
                return NotFound();
            }
        }
    }
}