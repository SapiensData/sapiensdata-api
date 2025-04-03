using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SapiensDataAPI.Data.DbContextCs;
using SapiensDataAPI.Dtos.Income.Request;
using SapiensDataAPI.Models;
using System.Security.Claims;

namespace SapiensDataAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class IncomesController(SapiensDataDbContext context, IMapper mapper, UserManager<ApplicationUser> userManager) : ControllerBase
	{
		private readonly SapiensDataDbContext _context = context;
		private readonly IMapper _mapper = mapper;
		private readonly UserManager<ApplicationUser> _userManager = userManager;

		// GET: api/Incomes
		[HttpGet]
		public async Task<ActionResult<IEnumerable<Income>>> GetIncomes()
		{
			return await _context.Incomes.ToListAsync();
		}

		// GET: api/Incomes/5
		[HttpGet("{id:int}")]
		public async Task<ActionResult<Income>> GetIncome(int id)
		{
			Income? income = await _context.Incomes.FindAsync(id);

			if (income == null)
			{
				return NotFound();
			}

			return income;
		}

		// PUT: api/Incomes/5
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPut("{id:int}")]
		[Authorize(Roles = "Admin")] // Temporary until the function is properly implemented
		public async Task<IActionResult> PutIncome(int id, Income income)
		{
			if (id != income.IncomeId)
			{
				return BadRequest();
			}

			_context.Entry(income).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!IncomeExists(id))
				{
					return NotFound();
				}

				throw;
			}

			return NoContent();
		}

		// POST: api/Incomes
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPost]
		[Authorize]
		public async Task<ActionResult<Income>> PostIncome(IncomeDto incomeDto)
		{
			string? username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(username))
			{
				return Unauthorized("User couldn't be identified.");
			}

			ApplicationUser? user = await _userManager.FindByNameAsync(username);
			if (user == null)
			{
				return NotFound("User not found.");
			}

			Income income = _mapper.Map<Income>(incomeDto);
			income.UserId = user.Id;
			await _context.Incomes.AddAsync(income);
			await _context.SaveChangesAsync();

			return CreatedAtAction("GetIncome", new { id = income.IncomeId }, income);
		}

		// DELETE: api/Incomes/5
		[HttpDelete("{id:int}")]
		[Authorize(Roles = "Admin")] // Temporary until the function is properly implemented
		public async Task<IActionResult> DeleteIncome(int id)
		{
			Income? income = await _context.Incomes.FindAsync(id);
			if (income == null)
			{
				return NotFound();
			}

			_context.Incomes.Remove(income);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		private bool IncomeExists(int id)
		{
			return _context.Incomes.Any(e => e.IncomeId == id);
		}
	}
}