using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SapiensDataAPI.Data.DbContextCs;
using SapiensDataAPI.Models;

namespace SapiensDataAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class EncryptionTestsController(SapeinsDataDbContext context) : ControllerBase
	{
		private readonly SapeinsDataDbContext _context = context;

		// GET: api/EncryptionTests
		[HttpGet]
		public async Task<ActionResult<IEnumerable<EncryptionTest>>> GetEncryptionTests()
		{
			return await _context.EncryptionTests.ToListAsync();
		}

		// GET: api/EncryptionTests/5
		[HttpGet("{id}")]
		public async Task<ActionResult<EncryptionTest>> GetEncryptionTest(int id)
		{
			EncryptionTest? encryptionTest = await _context.EncryptionTests.FindAsync(id);

			if (encryptionTest == null)
			{
				return NotFound();
			}

			return encryptionTest;
		}

		// PUT: api/EncryptionTests/5
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPut("{id}")]
		public async Task<IActionResult> PutEncryptionTest(int id, EncryptionTest encryptionTest)
		{
			if (id != encryptionTest.Id)
			{
				return BadRequest();
			}

			_context.Entry(encryptionTest).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!EncryptionTestExists(id))
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

		// POST: api/EncryptionTests
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPost]
		public async Task<ActionResult<EncryptionTest>> PostEncryptionTest(EncryptionTest encryptionTest)
		{
			_context.EncryptionTests.Add(encryptionTest);
			await _context.SaveChangesAsync();

			return CreatedAtAction("GetEncryptionTest", new { id = encryptionTest.Id }, encryptionTest);
		}

		// DELETE: api/EncryptionTests/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteEncryptionTest(int id)
		{
			EncryptionTest? encryptionTest = await _context.EncryptionTests.FindAsync(id);
			if (encryptionTest == null)
			{
				return NotFound();
			}

			_context.EncryptionTests.Remove(encryptionTest);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		private bool EncryptionTestExists(int id)
		{
			return _context.EncryptionTests.Any(e => e.Id == id);
		}
	}
}
