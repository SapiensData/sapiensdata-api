namespace SapiensDataAPI.Models
{
	public class UserRelationship
	{
		public int RelationshipId { get; set; }

		public required string UserId { get; set; }

		public required string RelatedUserId { get; set; }

		public string? RelationshipType { get; set; }

		public DateTime? CreatedAt { get; set; }

		public virtual ApplicationUser RelatedUser { get; set; } = null!;

		public virtual ApplicationUser User { get; set; } = null!;
	}
}