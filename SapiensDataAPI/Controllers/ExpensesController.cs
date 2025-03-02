﻿using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SapiensDataAPI.Data.DbContextCs;
using SapiensDataAPI.Dtos.Expense.Request;
using SapiensDataAPI.Models;

namespace SapiensDataAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ExpensesController(SapeinsDataDbContext context, IMapper mapper) : ControllerBase
	{
		private readonly SapeinsDataDbContext _context = context;
		private readonly IMapper _mapper = mapper;

		// GET: api/Expenses
		[HttpGet]
		public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses()
		{
			return await _context.Expenses.ToListAsync();
		}

		// GET: api/Expenses/5
		[HttpGet("{id}")]
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
		[HttpPut("{id}")]
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
				else
				{
					throw;
				}
			}

			return NoContent();
		}

		// POST: api/Expenses
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPost]
		public async Task<ActionResult<Expense>> PostExpense(ExpenseDto expenseDto)
		{
			Expense expense = _mapper.Map<Expense>(expenseDto);
			_context.Expenses.Add(expense);
			await _context.SaveChangesAsync();

			return CreatedAtAction("GetExpense", new { id = expense.ExpenseId }, expense);
		}

		// DELETE: api/Expenses/5
		[HttpDelete("{id}")]
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