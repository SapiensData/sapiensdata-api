using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SapiensDataAPI.Data.DbContextCs;
using SapiensDataAPI.Dtos.Expense.Request;
using SapiensDataAPI.Models;
using System.Security.Claims;

namespace SapiensDataAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ExpensesController(SapiensDataDbContext context, IMapper mapper, UserManager<ApplicationUser> userManager) : ControllerBase
	{
		private readonly SapiensDataDbContext _context = context;
		private readonly IMapper _mapper = mapper;
		private readonly UserManager<ApplicationUser> _userManager = userManager;

		// GET: api/Expenses
		[HttpGet]
		public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses()
		{
			return await _context.Expenses.ToListAsync();
		}

		// GET: api/Expenses/5
		[HttpGet("{id:int}")]
		public async Task<ActionResult<Expense>> GetExpense(int id)
		{
			Expense? expense = await _context.Expenses.FindAsync(id);

			if (expense == null)
			{
				return NotFound();
			}

			return expense;
		}

		// PUT: api/Expenses/5
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPut("{id:int}")]
		[Authorize(Roles = "Admin")] // Temporary until the function is properly implemented
		public async Task<IActionResult> PutExpense(int id, Expense expense)
		{
			if (id != expense.ExpenseId)
			{
				return BadRequest();
			}

			_context.Entry(expense).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!ExpenseExists(id))
				{
					return NotFound();
				}

				throw;
			}

			return NoContent();
		}

		// POST: api/Expenses
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPost]
		[Authorize]
		public async Task<ActionResult<Expense>> PostExpense(ExpenseDto expenseDto)
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

			Expense expense = _mapper.Map<Expense>(expenseDto);
			expense.UserId = user.Id;
			await _context.Expenses.AddAsync(expense);
			await _context.SaveChangesAsync();

			return CreatedAtAction("GetExpense", new { id = expense.ExpenseId }, expense);
		}

		// DELETE: api/Expenses/5
		[HttpDelete("{id:int}")]
		[Authorize(Roles = "Admin")] // Temporary until the function is properly implemented
		public async Task<IActionResult> DeleteExpense(int id)
		{
			Expense? expense = await _context.Expenses.FindAsync(id);
			if (expense == null)
			{
				return NotFound();
			}

			_context.Expenses.Remove(expense);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		private bool ExpenseExists(int id)
		{
			return _context.Expenses.Any(e => e.ExpenseId == id);
		}
	}
}