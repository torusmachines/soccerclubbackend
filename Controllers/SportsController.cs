using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FootballDashboardAPI.Models;

namespace FootballDashboardAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SportsController : ControllerBase
    {
        private readonly FootballContext _context;

        public SportsController(FootballContext context)
        {
            _context = context;
        }

        // GET: api/Sports
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sport>>> GetSports()
        {
            return await _context.Sports.ToListAsync();
        }

        // GET: api/Sports/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Sport>> GetSport(int id)
        {
            var sport = await _context.Sports.FindAsync(id);

            if (sport == null)
            {
                return NotFound();
            }

            return sport;
        }

        // PUT: api/Sports/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSport(int id, Sport sport)
        {
            sport.SportId = id;

            _context.Entry(sport).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SportExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(sport);
        }

        // POST: api/Sports
        [HttpPost]
        public async Task<ActionResult<Sport>> PostSport(Sport sport)
        {
            sport.CreatedAt = DateTime.UtcNow;

            _context.Sports.Add(sport);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw;
            }

            return CreatedAtAction("GetSport", new { id = sport.SportId }, sport);
        }

        // DELETE: api/Sports/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSport(int id)
        {
            var sport = await _context.Sports.FindAsync(id);
            if (sport == null)
            {
                return NotFound();
            }

            _context.Sports.Remove(sport);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SportExists(int id)
        {
            return _context.Sports.Any(e => e.SportId == id);
        }
    }
}