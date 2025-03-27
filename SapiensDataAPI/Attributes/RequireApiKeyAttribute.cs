using DotNetEnv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace SapiensDataAPI.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class RequireApiKeyAttribute : ActionFilterAttribute
	{
		private const string ApiKeyHeader = "Very-cool-api-key";

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeader, out StringValues apiKey))
			{
				context.Result = new UnauthorizedResult();
				return;
			}

			Env.Load(".env");
			string? expectedApiKey = Environment.GetEnvironmentVariable("SAPIENS_ANALYZER_SERVER_KEY");

			if (apiKey != expectedApiKey)
			{
				context.Result = new UnauthorizedResult();
				return;
			}

			base.OnActionExecuting(context);
		}
	}
}