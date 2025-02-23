using System.Threading.RateLimiting;

namespace SapiensDataAPI.Configs
{
	public class DefaultRateLimitOptions
	{
		public const string MyRateLimit = "DefaultRateLimit";
		public int PermitLimit { get; set; } = 100;
		public int Window { get; set; } = 10;
		public int ReplenishmentPeriod { get; set; } = 20;
		public int QueueLimit { get; set; } = 2;
		public int SegmentsPerWindow { get; set; } = 8;
		public int TokenLimit { get; set; } = 10;
		public int TokensPerPeriod { get; set; } = 4;
		public bool AutoReplenishment { get; set; }
		public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;
	}
}