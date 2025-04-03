namespace SapiensDataAPI.Models
{
	public class LabelAssignment
	{
		public int LabelAssignmentId { get; set; }

		public int LabelId { get; set; }

		public string EntityType { get; set; } = null!;

		public int EntityId { get; set; }

		public DateTime? AssignedAt { get; set; }

		public virtual Label Label { get; set; } = null!;
	}
}