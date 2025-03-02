using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity; // Import Identity for user and role management
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SapiensDataAPI.Attributes;
using SapiensDataAPI.Configs;
using SapiensDataAPI.Data.DbContextCs;
using SapiensDataAPI.Models; // Import models, including ApplicationUserModel
using SapiensDataAPI.Services.ByteArrayPlainTextConverter;
using SapiensDataAPI.Services.GlobalVariable;
using SapiensDataAPI.Services.JwtToken; // Import services, including JwtTokenService
using System.Text; // Import for encoding JWT secret key

WebApplicationBuilder builder = WebApplication.CreateBuilder(args); // Create a builder for the web application
builder.Configuration.AddEnvironmentVariables(); // Add environment variables to the configuration

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());  // Registers all profiles in the project

// Load environment variables from .env file
Env.Load(".env");

//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.Listen(System.Net.IPAddress.Any, 7198);
//});

// Add services to the container
builder.Services.AddControllers(); // Add controllers to handle API routes
builder.Services.AddEndpointsApiExplorer(); // Add API explorer for endpoints

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddScoped<RequireApiKeyAttribute>();
builder.Services.AddSingleton<GlobalVariableService>();

builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
	// Configure JWT Authorization in Swagger
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Description = "Please enter JWT with Bearer prefix 'Bearer {token}': ",
		Name = "Authorization",
		Type = SecuritySchemeType.ApiKey,
		Scheme = "Bearer"
	});
	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});
});

builder.Services.AddSwaggerGen(c =>
{
	// Add a parameter for the API key in Swagger UI
	c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Name = "Very-cool-api-key",
		Type = SecuritySchemeType.ApiKey,
		Description = "API Key needed to access the python json endpoint"
	});

	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "ApiKey"
				}
			},
			Array.Empty<string>()
		}
	});
});

// Configure CORS
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowLocalNetwork", // Define a CORS policy named "AllowLocalhost"
		builder =>
		{
			//builder.WithOrigins("http://localhost:4892", "https://localhost:4891", "https://0.0.0.0:7198") // Allow frontend URL origins
			builder.AllowAnyOrigin()
				   .AllowAnyHeader() // Allow any headers in CORS requests
				   .AllowAnyMethod(); // Allow any HTTP methods
		});
});

string? dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//dbConnectionString = dbConnectionString.Replace("${DB_SERVER_IP}", Environment.GetEnvironmentVariable("DB_SERVER_IP"));
string? dbServerIp = Environment.GetEnvironmentVariable("DB_SERVER_IP");

if (dbConnectionString != null && dbServerIp != null)
{
	dbConnectionString = dbConnectionString.Replace("${DB_SERVER_IP}", dbServerIp);
}
else
{
	// Handle the case where either dbConnectionString or dbServerIp is null
	Console.WriteLine("Either dbConnectionString or DB_SERVER_IP is not set.");
	throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
}

// Configure the database context
builder.Services.AddDbContext<SapeinsDataDbContext>(options =>
	options.UseSqlServer(dbConnectionString)); // Configure SQL Server with a connection string

// Configure Identity
builder.Services.AddIdentity<ApplicationUserModel, IdentityRole>() // Add Identity services for ApplicationUserModel and roles
	.AddEntityFrameworkStores<SapeinsDataDbContext>() // Use the database context for storing identity data
	.AddDefaultTokenProviders(); // Add default token providers for password reset and other identity features

// Configure JWT Key, Issuer, and Audience from environment variables
builder.Configuration["Jwt:Key"] = Env.GetString("JWT_KEY") ?? throw new InvalidOperationException("JWT Key is missing"); // Get JWT key from .env

// JWT Config
string? jwtKey = builder.Configuration["Jwt:Key"]; // Store the JWT key in a variable
string? jwtIssuer = builder.Configuration["Jwt:Issuer"]; // Store the JWT issuer in a variable
string? jwtAudience = builder.Configuration["Jwt:Audience"]; // Store the JWT audience in a variable

if (string.IsNullOrEmpty(jwtKey))
{
	throw new InvalidOperationException("JWT Key is not configured in the settings.");
}

// JWT Validation params
byte[] keyBytes = Encoding.UTF8.GetBytes(jwtKey); // Convert the JWT key into a byte array
TokenValidationParameters tokenValidationParams = new()
{
	ValidateIssuerSigningKey = true, // Ensure the token is signed with the correct key
	IssuerSigningKey = new SymmetricSecurityKey(keyBytes), // Use the symmetric security key for validation
	ValidateIssuer = true, // Validate the token's issuer
	ValidIssuer = jwtIssuer, // Set the valid issuer
	ValidateAudience = true, // Validate the token's audience
	ValidAudience = jwtAudience, // Set the valid audience
	ValidateLifetime = true, // Ensure the token has not expired
};

// Add Authentication
builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // Set the default authentication scheme to JWT Bearer
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // Set the default challenge scheme to JWT Bearer
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = tokenValidationParams; // Use the defined token validation parameters
	options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
	options.Events = new JwtBearerEvents // Set up event handling for JWT Bearer authentication
	{
		OnAuthenticationFailed = context =>
		{
			context.Response.Headers.Append("Token-Error", "Invalid token"); // Add a custom header for token errors
			return Task.CompletedTask; // Complete the task after handling the error
		},
		OnTokenValidated = context =>
		{
			// You can add additional validation logic here if needed
			return Task.CompletedTask; // Complete the task after token validation
		}
	};
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
	options.JsonSerializerOptions.Converters.Add(new ByteArrayPlainTextConverterService());
});

string? defaultRateLimitName = "default";
DefaultRateLimitOptions defaultRateLimits = new();
builder.Configuration.GetSection(DefaultRateLimitOptions.MyRateLimit).Bind(defaultRateLimits);

builder.Services.AddRateLimiter(options =>
{
	options.AddTokenBucketLimiter(policyName: defaultRateLimitName, options =>
	{
		options.TokenLimit = defaultRateLimits.TokenLimit;
		options.QueueProcessingOrder = defaultRateLimits.QueueProcessingOrder;
		options.QueueLimit = defaultRateLimits.QueueLimit;
		options.ReplenishmentPeriod = TimeSpan.FromSeconds(defaultRateLimits.ReplenishmentPeriod);
		options.TokensPerPeriod = defaultRateLimits.TokensPerPeriod;
		options.AutoReplenishment = defaultRateLimits.AutoReplenishment;
	});
});

// Authorization Policies
string[] roles = ["SuperAdmin", "Admin", "NormalUser", "TeamLead", "Guest", "Moderator", "Developer", "Tester", "DataScientist"];

foreach (string role in roles)
{
	builder.Services.AddAuthorizationBuilder()
		.AddPolicy(role, policy => policy.RequireRole(role));
}

// Add Scoped services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>(); // Add JwtTokenService to the service collection with Scoped lifetime

WebApplication app = builder.Build(); // Build the application

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment()) // Check if the application is in development environment
{
	app.UseSwagger(); // Enable Swagger for API documentation
	app.UseSwaggerUI(); // Enable the Swagger UI
}

// Create roles on startup
using (IServiceScope scope = app.Services.CreateScope()) // Create a scope for dependency injection
{
	RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(); // Get the RoleManager service
																												   //var roles = new[] { "Admin", "NormalUser", "SuperAdmin", "Moderator", "TeamLead", "Developer", "Tester", "Guest", "DataScientist" }; // Define a list of roles
	foreach (string role in roles)
	{
		if (!await roleManager.RoleExistsAsync(role)) // Check if the role exists
		{
			await roleManager.CreateAsync(new IdentityRole(role)); // Create the role if it doesn't exist
		}
	}
}

app.UseRateLimiter(); // Use the RateLimiter middleware

app.UseHttpsRedirection(); // Redirect all HTTP requests to HTTPS

app.UseCors("AllowLocalNetwork"); // use the CORS policy defined earlier

app.UseAuthentication(); // Enable Authentication middleware
app.UseAuthorization(); // Enable Authorization middleware

app.MapControllers().RequireRateLimiting(defaultRateLimitName); // Map controller routes

app.Run(); // Run the application