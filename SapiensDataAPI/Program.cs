using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SapiensDataAPI.Attributes;
using SapiensDataAPI.Configs;
using SapiensDataAPI.Data.DbContextCs;
using SapiensDataAPI.Models;
using SapiensDataAPI.Services.ByteArrayPlainTextConverter;
using SapiensDataAPI.Services.GlobalVariable;
using SapiensDataAPI.Services.JwtToken;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

Env.Load(".env");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddScoped<RequireApiKeyAttribute>();
builder.Services.AddSingleton<GlobalVariableService>();

builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
	c.AddSecurityDefinition("Bearer",
		new OpenApiSecurityScheme
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
			new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
			Array.Empty<string>()
		}
	});
});

builder.Services.AddSwaggerGen(c =>
{
	c.AddSecurityDefinition("ApiKey",
		new OpenApiSecurityScheme
		{
			In = ParameterLocation.Header,
			Name = "Very-cool-api-key",
			Type = SecuritySchemeType.ApiKey,
			Description = "API Key needed to access the python json endpoint"
		});

	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } },
			Array.Empty<string>()
		}
	});
});

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowEverything",
		corsPolicyBuilder =>
		{
			//builder.WithOrigins("http://localhost:4892", "https://localhost:4891", "https://0.0.0.0:7198") // Allow frontend URL origins
			corsPolicyBuilder
				.AllowAnyOrigin()
				.AllowAnyHeader()
				.AllowAnyMethod();
		});
});

string? dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
string? dbServerIp = Environment.GetEnvironmentVariable("DB_SERVER_IP");
if (dbConnectionString != null && dbServerIp != null)
{
	dbConnectionString = dbConnectionString.Replace("${DB_SERVER_IP}", dbServerIp);
}
else
{
	Console.WriteLine("Either dbConnectionString or DB_SERVER_IP is not set.");
	throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
}

builder.Services.AddDbContext<SapiensDataDbContext>(options =>
	options.UseSqlServer(dbConnectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
	.AddEntityFrameworkStores<SapiensDataDbContext>() // Use the database context for storing identity data
	.AddDefaultTokenProviders(); // Add default token providers for password reset and other identity features

builder.Configuration["Jwt:Key"] = Env.GetString("JWT_KEY") ?? throw new InvalidOperationException("JWT Key is missing");

// JWT Config
string? jwtKey = builder.Configuration["Jwt:Key"];
string? jwtIssuer = builder.Configuration["Jwt:Issuer"];
string? jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey))
{
	throw new InvalidOperationException("JWT Key is not configured in the settings.");
}

// JWT Validation params
byte[] keyBytes = Encoding.UTF8.GetBytes(jwtKey);
TokenValidationParameters tokenValidationParams = new()
{
	ValidateIssuerSigningKey = true,
	IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
	ValidIssuer = jwtIssuer,
	ValidAudience = jwtAudience,
	ValidateIssuer = true,
	ValidateAudience = true,
	ValidateLifetime = true
};

builder.Services.AddAuthentication(options =>
	{
		options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
		options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
	})
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = tokenValidationParams;
		options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
		// Set up event handling for JWT Bearer authentication
		options.Events = new JwtBearerEvents
		{
			OnAuthenticationFailed = context =>
			{
				context.Response.Headers.Append("Token-Error", "Invalid token");
				return Task.CompletedTask;
			},
			OnTokenValidated = _ => Task.CompletedTask
		};
	});

builder.Services.AddControllers().AddJsonOptions(options =>
{
	options.JsonSerializerOptions.Converters.Add(new ByteArrayPlainTextConverterService());
});

const string defaultRateLimitName = "default";
DefaultRateLimitOptions defaultRateLimits = new();
builder.Configuration.GetSection(DefaultRateLimitOptions.MyRateLimit).Bind(defaultRateLimits);

builder.Services.AddRateLimiter(options =>
{
	options.AddTokenBucketLimiter(defaultRateLimitName, rateLimiterOptions =>
	{
		rateLimiterOptions.TokenLimit = defaultRateLimits.TokenLimit;
		rateLimiterOptions.QueueProcessingOrder = defaultRateLimits.QueueProcessingOrder;
		rateLimiterOptions.QueueLimit = defaultRateLimits.QueueLimit;
		rateLimiterOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(defaultRateLimits.ReplenishmentPeriod);
		rateLimiterOptions.TokensPerPeriod = defaultRateLimits.TokensPerPeriod;
		rateLimiterOptions.AutoReplenishment = defaultRateLimits.AutoReplenishment;
	});
});

// Authorization Policies
string[] roles = ["SuperAdmin", "Admin", "NormalUser", "TeamLead", "Guest", "Moderator", "Developer", "Tester", "DataScientist"];

foreach (string role in roles)
{
	builder.Services.AddAuthorizationBuilder()
		.AddPolicy(role, policy => policy.RequireRole(role));
}

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

// Create roles on startup
// Create a scope for dependency injection
using (IServiceScope scope = app.Services.CreateScope())
{
	RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
	foreach (string role in roles)
	{
		if (!await roleManager.RoleExistsAsync(role))
		{
			await roleManager.CreateAsync(new IdentityRole(role));
		}
	}
}

app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseCors("AllowEverything");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting(defaultRateLimitName);

await app.RunAsync();