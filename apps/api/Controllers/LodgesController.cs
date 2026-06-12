using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Models;
using Api.Data;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LodgesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LodgesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Lodges
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Lodge>>> GetLodges()
        {
            return await _context.Lodges.ToListAsync();
        }

        // GET: api/Lodges/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Lodge>> GetLodge(long id)
        {
            var lodge = await _context.Lodges.FindAsync(id);

            if (lodge == null)
            {
                return NotFound();
            }

            return lodge;
        }

        // PUT: api/Lodges/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLodge(long id, Lodge lodge)
        {
            if (id != lodge.Id)
            {
                return BadRequest();
            }

            _context.Entry(lodge).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LodgeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Lodges
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Lodge>> PostLodge(Lodge lodge)
        {
            _context.Lodges.Add(lodge);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLodge), new { id = lodge.Id }, lodge);
        }

        // DELETE: api/Lodges/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLodge(long id)
        {
            var lodge = await _context.Lodges.FindAsync(id);
            if (lodge == null)
            {
                return NotFound();
            }

            _context.Lodges.Remove(lodge);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool LodgeExists(long id)
        {
            return _context.Lodges.Any(e => e.Id == id);
        }
    }
}
