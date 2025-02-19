using DotNetEnv;  // Import your ApiSettings class
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SapiensDataAPI.Attributes
{
	public class RequireApiKeyAttribute : ActionFilterAttribute
	{
		private IConfiguration? _configuration;

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			// Get the IConfiguration instance using the IServiceProvider
			_configuration ??= context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

			if (context.HttpContext.Request.Headers.TryGetValue("Very-cool-api-key", out var apiKey))
			{
				Env.Load(".env");
				var expectedApiKey = Environment.GetEnvironmentVariable("SAPIENS_ANALYZER_SERVER_KEY");

				if (apiKey != expectedApiKey)
				{
					context.Result = new UnauthorizedResult(); // Unauthorized if API key is incorrect
					return;
				}
			}
			else
			{
				context.Result = new UnauthorizedResult(); // Unauthorized if header is missing
				return;
			}

			base.OnActionExecuting(context);
		}
	}
}