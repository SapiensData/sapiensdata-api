namespace SapiensDataAPI.Models;

public partial class UnitType
{
	public int UnitId { get; set; }

	public string? UnitName { get; set; }

	public string? UnitType1 { get; set; }

	public virtual ICollection<Product> ProductIppUnitNavigations { get; set; } = [];

	public virtual ICollection<Product> ProductQUnitNavigations { get; set; } = [];

	public virtual ICollection<Product> ProductUpUnitNavigations { get; set; } = [];

	public virtual ICollection<Product> ProductVUnitNavigations { get; set; } = [];

	public virtual ICollection<Product> ProductWUnitNavigations { get; set; } = [];
}
