﻿using AutoMapper;
using DotNetEnv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SapiensDataAPI.Attributes;
using SapiensDataAPI.Data.DbContextCs;
using SapiensDataAPI.Dtos;
using SapiensDataAPI.Dtos.ImageUploader.Request;
using SapiensDataAPI.Dtos.Receipt.JSON;
using SapiensDataAPI.Dtos.Receipt.Response;
using SapiensDataAPI.Models;
using SapiensDataAPI.Services.JwtToken;
using System.Globalization;
using System.Text.Json;

namespace SapiensDataAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ReceiptsController(SapeinsDataDbContext context, IJwtTokenService jwtTokenService, IMapper mapper, UserManager<ApplicationUserModel> userManager) : ControllerBase
	{
		private readonly SapeinsDataDbContext _context = context;
		private readonly IJwtTokenService _jwtTokenService = jwtTokenService; // Dependency injection for handling JWT token generation
		private readonly IMapper _mapper = mapper;
		private readonly UserManager<ApplicationUserModel> _userManager = userManager;

		// GET: api/Receipts
		[HttpGet]
		[Authorize]
		public async Task<ActionResult<IEnumerable<Receipt>>> GetReceipts()
		{
			return await _context.Receipts.ToListAsync();
		}

		// GET: api/Receipts
		[HttpPost("receive-json-from-python")]
		[RequireApiKey]
		public async Task<IActionResult> ReceiveJSON([FromBody] ReceiptVailidation receiptVailidation, [FromHeader] string username)
		{
			Env.Load(".env");

			string? googleDrivePath = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_BEGINNING_PATH");
			if (googleDrivePath == null)
			{
				return StatusCode(500, "Google Drive path doesn't exist in .env file.");
			}

			if (username.Contains("..") || username.Contains('/') || username.Contains('\\'))
			{
				return BadRequest("Invalid username. Username cannot contain '..' or '/' or '\\'.");
			}

			//var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "SapiensCloud", "src", "media", "UserReceiptUploads", JwtPayload.Sub);
			string filePath = Path.Combine(googleDrivePath, "SapiensCloud", "media", "user_data", username, "receipts", receiptVailidation.FileMetadata.ReceiptFilename);
			if (!await Task.Run(() => System.IO.File.Exists(filePath)))
			{
				return BadRequest("File doesn't exist");
			}

			string? directory = Path.GetDirectoryName(filePath) ?? null;
			if (directory == null || !await Task.Run(() => Directory.Exists(directory)))
			{
				return BadRequest("Directory is not ok");
			}

			string correctedStoreName = receiptVailidation.Store.Name.Replace(' ', '-');

			string extension = Path.GetExtension(filePath);
			string newFileName = correctedStoreName + "_" + receiptVailidation.Receipt.BuyDatetime.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + extension;

			// Create the new path by combining the directory and new file name
			string newPath = Path.Combine(directory, newFileName);

			if (await Task.Run(() => System.IO.File.Exists(newPath)))
			{
				return BadRequest("File already exists");
			}

			// Rename the file by moving it to the new path
			await Task.Run(() => System.IO.File.Move(filePath, newPath));

			string[] pathSegments = filePath.Split(Path.DirectorySeparatorChar);
			string lastThreeSegments = string.Join(Path.DirectorySeparatorChar.ToString(), pathSegments.TakeLast(3));

			ApplicationUserModel? user = await _userManager.FindByNameAsync(username);
			if (user == null)
			{
				// Handle the case where the user is not found
				return NotFound("User not found");
			}

			List<Receipt> receipts = await _context.Receipts
				.Where(r => r.ReceiptImagePath != null && r.ReceiptImagePath.EndsWith(lastThreeSegments) && r.UserId == user.Id)
				.ToListAsync();

			if (receipts.Count == 0)
			{
				return NotFound("No receipts found");
			}

			if (receipts.Count > 1)
			{
				return BadRequest($"Too many receipts, should be 1 but were {receipts.Count}");
			}

			if (receiptVailidation.Product.Count == 0)
			{
				return BadRequest("No products found");
			}

			_mapper.Map(receiptVailidation.Receipt, receipts[0]);
			receipts[0].ReceiptImagePath = newPath;

			List<Product> products = new(receiptVailidation.Product.Count);
			foreach (ProductV product in receiptVailidation.Product)
			{
				products.Add(_mapper.Map<Product>(product));
			}

			await _context.Products.AddRangeAsync(products);

			List<int> productIds = [.. products.Select(p => p.ProductId)];

			List<ReceiptProduct> receiptProducts = new(receiptVailidation.Product.Count);
			foreach (int item in productIds)
			{
				receiptProducts.Add(new ReceiptProduct
				{
					ReceiptId = receipts[0].ReceiptId,
					ProductId = item
				});
			}

			await _context.ReceiptProducts.AddRangeAsync(receiptProducts);

			TaxRate taxRate = _mapper.Map<TaxRate>(receiptVailidation.TaxRate);
			taxRate.ReceiptId = receipts[0].ReceiptId;
			await _context.AddAsync(taxRate);

			ReceiptTaxDetail receiptTaxDetails = _mapper.Map<ReceiptTaxDetail>(receiptVailidation.ReceiptTaxDetail);
			receiptTaxDetails.ReceiptId = receipts[0].ReceiptId;
			receiptTaxDetails.TaxRateId = taxRate.TaxRateId;
			await _context.AddAsync(receiptTaxDetails);

			Address address = _mapper.Map<Address>(receiptVailidation.Store);
			await _context.AddAsync(address);

			Store store = _mapper.Map<Store>(receiptVailidation.Store);
			await _context.AddAsync(store);

			receipts[0].StoreId = store.StoreId;
			_context.Update(receipts[0]);

			StoreAddress storeAddress = new()
			{
				StoreId = store.StoreId,
				AddressId = address.AddressId,
				AddressType = receiptVailidation.Store.AddressType
			};
			await _context.AddAsync(storeAddress);

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!ReceiptExists(receipts[0].ReceiptId))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return StatusCode(501, "Python path isn't implemented");

			/*string workingDirectory = @"../../Analytics/";
			if (!Directory.Exists(workingDirectory))
			{
				return BadRequest($"Working directory not found: {workingDirectory}");
			}

			//string pythonExePath = "../../Analytics/venv/Scripts/python.exe";
			string pythonExePath = "venv/Scripts/python.exe";
			if (!System.IO.File.Exists(workingDirectory + pythonExePath))
			{
				return BadRequest("venv Python isn't there");
			}

			//string pythonScriptPath = @"../../Analytics/src/main.py";  // Path to the Python script
			string pythonScriptPath = @"src/main.py";  // Path to the Python script

			if (!System.IO.File.Exists(workingDirectory + pythonScriptPath))
			{
				return BadRequest("Python script isn't there");
			}

			var parameter = username;
			if (parameter == null)
			{
				return BadRequest("image path is null");
			}

			pythonExePath = workingDirectory + "venv/Scripts/python.exe";

			// Set up the process start info
			ProcessStartInfo startInfo2 = new()
			{
				FileName = pythonExePath,  // Path to Python
				Arguments = $"\"{pythonScriptPath}\" \"{parameter}\"",  // Arguments (Python script and parameter)
				RedirectStandardOutput = true,  // Redirect output to capture it
				UseShellExecute = false,  // Do not use shell execution (to redirect output)
				CreateNoWindow = true,  // Don't create a command prompt window
				WorkingDirectory = @"../../Analytics/"  // Set the desired working directory
			};

			// Start the process
			using (Process? process = Process.Start(startInfo2))  // Nullable Process type
			{
				if (process == null)
				{
					// If the process is null, return an Internal Server Error (500)
					return StatusCode(500, "Failed to start the Python process.");
				}
			}

			return Ok("Image is ok and updated");*/
		}

		// GET: api/Receipts/5
		[HttpGet("{offset}")]
		[Authorize]
		public async Task<ActionResult<ResReceiptDto>> GetReceipt(int offset = 0)
		{
			string token = HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
			JsonElement decodedToken = _jwtTokenService.DecodeJwtPayloadToJson(token).RootElement;
			JwtPayload? JwtPayload = JsonSerializer.Deserialize<JwtPayload>(decodedToken) ?? null;
			if (JwtPayload == null)
			{
				return BadRequest("JwtPayload is not ok.");
			}

			ApplicationUserModel? user = await _userManager.FindByNameAsync(JwtPayload.Sub);
			if (user == null)
			{
				// Handle the case where the user is not found
				return NotFound("User not found");
			}

			Receipt? receipt = await _context.Receipts
				.Where(r => r.UserId == user.Id)
				.OrderByDescending(r => r.UploadDate)
				.Skip(offset)
				.Take(1)
				.Include(r => r.Store)
				.FirstOrDefaultAsync();

			if (receipt == null)
			{
				return NotFound();
			}

			if (!System.IO.File.Exists(receipt.ReceiptImagePath))
			{
				return NotFound();
			}

			StoreAddress? storeAddress = await _context.StoreAddresses
				.Where(sa => sa.StoreId == receipt.StoreId)
				.FirstOrDefaultAsync();
			if (storeAddress == null)
			{
				return BadRequest("No storeaddress found");
			}

			Address? address = await _context.Addresses
				.Where(a => a.AddressId == storeAddress.AddressId)
				.FirstOrDefaultAsync();
			if (address == null)
			{
				return BadRequest("No address found");
			}

			StoreV store = _mapper.Map<StoreV>(receipt.Store);
			store = _mapper.Map(address, store);

			// Determine the MIME type based on the file extension
			FileExtensionContentTypeProvider provider = new();
			if (!provider.TryGetContentType(receipt.ReceiptImagePath, out string? contentType))
			{
				return BadRequest("Content type of the image can not be determined");
			}

			byte[] imageBytes = System.IO.File.ReadAllBytes(receipt.ReceiptImagePath);
			string base64Image = Convert.ToBase64String(imageBytes);

			List<ReceiptProduct> productsReceipts = await _context.ReceiptProducts
				.Where(rp => rp.ReceiptId == receipt.ReceiptId)
				.ToListAsync();
			List<int> productsReceiptsProductsIds = [.. productsReceipts.Select(p => p.ProductId)];

			List<Product> products = await _context.Products
				.Where(p => productsReceiptsProductsIds.Contains(p.ProductId))
				.ToListAsync();

			List<ProductV> productVs = new(products.Count);

			foreach (Product? product in products)
			{
				productVs.Add(_mapper.Map<ProductV>(product));
			}

			List<TaxRate> taxRates = await _context.TaxRates
				.Where(tr => tr.ReceiptId == receipt.ReceiptId)
				.ToListAsync();

			List<TaxRateV> taxRatesVs = new(taxRates.Count);

			foreach (TaxRate? taxRate in taxRates)
			{
				taxRatesVs.Add(_mapper.Map<TaxRateV>(taxRate));
			}

			List<ReceiptTaxDetail> receiptTaxDetails = await _context.ReceiptTaxDetails
				.Where(trd => trd.ReceiptId == receipt.ReceiptId)
				.ToListAsync();

			List<ReceiptTaxDetailV> receiptTaxDetailVs = new(receiptTaxDetails.Count);

			foreach (ReceiptTaxDetail? receiptTaxDetail in receiptTaxDetails)
			{
				receiptTaxDetailVs.Add(_mapper.Map<ReceiptTaxDetailV>(receiptTaxDetail));
			}

			ResReceiptDto ret = new()
			{
				FileName = Path.GetFileName(receipt.ReceiptImagePath),
				ContentType = contentType,
				Store = store,
				Product = productVs,
				Receipt = _mapper.Map<ReceiptV>(receipt),
				TaxRate = taxRatesVs,
				ReceiptTaxDetail = receiptTaxDetailVs,
				ImageData = base64Image,
				UploadDate = receipt.UploadDate
			};

			return Ok(ret);
		}

		// PUT: api/Receipts/5
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPut("{id}")]
		[Authorize(Policy = "Admin")]
		public async Task<IActionResult> PutReceipt(int id, Receipt receipt)
		{
			if (id != receipt.ReceiptId)
			{
				return BadRequest();
			}

			_context.Entry(receipt).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!ReceiptExists(id))
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

		// POST: api/Receipts
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPost]
		[Authorize]
		public async Task<IActionResult> PostReceipt([FromForm] UploadImageDto image)
		{
			string token = HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
			JsonElement decodedToken = _jwtTokenService.DecodeJwtPayloadToJson(token).RootElement;
			JwtPayload? JwtPayload = JsonSerializer.Deserialize<JwtPayload>(decodedToken) ?? null;
			if (JwtPayload == null)
			{
				return BadRequest("JwtPayload is not ok.");
			}

			if (image == null || image.Image.Length == 0)
				return BadRequest("No image file provided.");

			await Task.CompletedTask; // Delete this when the python path is implemented

			return StatusCode(501, "Python path isn't implemented");

			/*Env.Load(".env");
			var googleDrivePath = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_BEGINNING_PATH");
			if (googleDrivePath == null)
			{
				return StatusCode(500, "Google Drive path doesn't exist in .env file.");
			}

			//var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "SapiensCloud", "src", "media", "UserReceiptUploads", JwtPayload.Sub);
			var uploadsFolderPath = Path.Combine(googleDrivePath, "SapiensCloud", "media", "user_data", JwtPayload.Sub, "receipts");

			if (!Directory.Exists(uploadsFolderPath))
			{
				try
				{
					Directory.CreateDirectory(uploadsFolderPath);
				}
				catch
				{
					return StatusCode(500, "Can't create directory.");
				}
			}

			var extension = Path.GetExtension(image.Image.FileName);
			var newFileName = "to_process_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + extension;

			var filePath = Path.Combine(uploadsFolderPath, newFileName);

			using (var fileStream = new FileStream(filePath, FileMode.Create))
			{
				await image.Image.CopyToAsync(fileStream);
			}

			var user = await _userManager.FindByNameAsync(JwtPayload.Sub);
			if (user == null)
			{
				return NotFound("User was not found.");
			}
			var userId = user.Id;

			var EXAMPLEReceiptDto = new ReceiptDto
			{
				BuyDatetime = DateTime.UtcNow,
				TraceNumber = "ABC12345",
				TotalAmount = 150.00m,
				CashbackAmount = 5.00m,
				Currency = "USD",
				FullNamePaymentMethod = "Credit Card",
				Iban = "US12345678901234567890",
				ReceiptImagePath = filePath,
				UserId = userId
			};

			var receipt = _mapper.Map<Receipt>(EXAMPLEReceiptDto);
			_context.Receipts.Add(receipt);
			await _context.SaveChangesAsync();

			string workingDirectory = @"../../Analytics/";
			if (!Directory.Exists(workingDirectory))
			{
				return BadRequest($"Working directory not found: {workingDirectory}");
			}

			//string pythonExePath = "../../Analytics/venv/Scripts/python.exe";
			string pythonExePath = "venv/Scripts/python.exe";
			if (!System.IO.File.Exists(workingDirectory + pythonExePath))
			{
				return BadRequest("venv Python isn't there");
			}

			//string pythonScriptPath = @"../../Analytics/src/main.py";  // Path to the Python script
			string pythonScriptPath = @"src/main.py";  // Path to the Python script

			if (!System.IO.File.Exists(workingDirectory + pythonScriptPath))
			{
				return BadRequest("Python script isn't there");
			}

			var parts = filePath.Split(Path.DirectorySeparatorChar);
			var lastParts = parts.Skip(Math.Max(0, parts.Length - 6));
			var pathForPython = Path.Combine([.. lastParts]);
			pathForPython = pathForPython.Replace("\\", "/");

			string? parameter = pathForPython ?? null;  // The parameter you want to pass to the Python script

			if (parameter == null)
			{
				return BadRequest("image path is null");
			}

			pythonExePath = workingDirectory + "venv/Scripts/python.exe";

			// Set up the process start info
			ProcessStartInfo startInfo2 = new()
			{
				FileName = pythonExePath,  // Path to Python
				Arguments = $"\"{pythonScriptPath}\" \"{parameter}\"",  // Arguments (Python script and parameter)
				RedirectStandardOutput = true,  // Redirect output to capture it
				UseShellExecute = false,  // Do not use shell execution (to redirect output)
				CreateNoWindow = true,  // Don't create a command prompt window
				WorkingDirectory = @"../../Analytics/"  // Set the desired working directory
			};

			// Start the process
			using (Process? process = Process.Start(startInfo2))  // Nullable Process type
			{
				if (process == null)
				{
					// If the process is null, return an Internal Server Error (500)
					return StatusCode(500, "Failed to start the Python process.");
				}
			}

			return Ok("Image uploaded successfully.");*/
		}

		// DELETE: api/Receipts/5
		[HttpDelete("{id}")]
		[Authorize(Policy = "Admin")]
		public async Task<IActionResult> DeleteReceipt(int id)
		{
			Receipt? receipt = await _context.Receipts.FindAsync(id);
			if (receipt == null)
			{
				return NotFound();
			}

			_context.Receipts.Remove(receipt);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		private bool ReceiptExists(int id)
		{
			return _context.Receipts.Any(e => e.ReceiptId == id);
		}
	}
}