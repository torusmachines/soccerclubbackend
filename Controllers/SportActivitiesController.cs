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
    public class SportActivitiesController : ControllerBase
    {
        private readonly FootballContext _context;

        public SportActivitiesController(FootballContext context)
        {
            _context = context;
        }

        // GET: api/SportActivities
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SportActivity>>> GetSportActivities()
        {
            return await _context.SportActivities.Include(sa => sa.Sport).ToListAsync();
        }

        // GET: api/SportActivities/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SportActivity>> GetSportActivity(int id)
        {
            var sportActivity = await _context.SportActivities.Include(sa => sa.Sport).FirstOrDefaultAsync(sa => sa.ActivityId == id);

            if (sportActivity == null)
            {
                return NotFound();
            }

            return sportActivity;
        }

        // GET: api/SportActivities/BySport/5
        [HttpGet("BySport/{sportId}")]
        public async Task<ActionResult<IEnumerable<SportActivity>>> GetSportActivitiesBySportId(int sportId)
        {
            var sportActivities = await _context.SportActivities
                .Include(sa => sa.Sport)
                .Where(sa => sa.SportId == sportId)
                .ToListAsync();

            if (sportActivities == null || !sportActivities.Any())
            {
                return NotFound();
            }

            return sportActivities;
        }

        // PUT: api/SportActivities/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSportActivity(int id, SportActivity sportActivity)
        {
            sportActivity.ActivityId = id;

            _context.Entry(sportActivity).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SportActivityExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(sportActivity);
        }

        // POST: api/SportActivities
        [HttpPost]
        public async Task<ActionResult<SportActivity>> PostSportActivity(SportActivity sportActivity)
        {
            sportActivity.CreatedAt = DateTime.UtcNow;

            _context.SportActivities.Add(sportActivity);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw;
            }

            return CreatedAtAction("GetSportActivity", new { id = sportActivity.ActivityId }, await _context.SportActivities.Include(sa => sa.Sport).FirstOrDefaultAsync(sa => sa.ActivityId == sportActivity.ActivityId));
        }

        // DELETE: api/SportActivities/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSportActivity(int id)
        {
            var sportActivity = await _context.SportActivities.FindAsync(id);
            if (sportActivity == null)
            {
                return NotFound();
            }

            _context.SportActivities.Remove(sportActivity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SportActivityExists(int id)
        {
            return _context.SportActivities.Any(e => e.ActivityId == id);
        }
    }
}