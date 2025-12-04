using Guessnica_backend.Dtos.Location;
using Guessnica_backend.Models;
using Guessnica_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guessnica_backend.Controllers;

    [ApiController]
    [Route("locations")]
    [Produces("application/json")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _service;

        public LocationController(ILocationService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize] 
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items.Select(x => new LocationResponseDto
            {
                Id = x.Id,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                ImageUrl = x.ImageUrl
            }));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> Get(int id)
        {
            var loc = await _service.GetByIdAsync(id);
            if (loc == null) return NotFound();

            return Ok(new LocationResponseDto
            {
                Id = loc.Id,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                ImageUrl = loc.ImageUrl
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] LocationCreateDto dto)
        {
            var loc = await _service.CreateAsync(new Location
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                ImageUrl = dto.ImageUrl
            });

            return CreatedAtAction(nameof(Get), new { id = loc.Id }, new LocationResponseDto
            {
                Id = loc.Id,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                ImageUrl = loc.ImageUrl
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] LocationUpdateDto dto)
        {
            var updated = await _service.UpdateAsync(id, new Location
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                ImageUrl = dto.ImageUrl
            });

            if (updated == null) return NotFound();

            return Ok(new LocationResponseDto
            {
                Id = updated.Id,
                Latitude = updated.Latitude,
                Longitude = updated.Longitude,
                ImageUrl = updated.ImageUrl
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? Ok() : NotFound();
        }
    }