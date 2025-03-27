namespace SapiensDataAPI.Services.GlobalVariable
{
	public class GlobalVariableService
	{
		public string SymmetricKey { get; } = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ??
		                                      throw new InvalidOperationException("Encryption key is not configured properly.");
	}
}