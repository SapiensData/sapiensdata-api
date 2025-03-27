using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace SapiensDataAPI.Provider.UuidV7Provider
{
	public class UuidV7Provider : ValueGenerator<Guid>
	{
		public override bool GeneratesTemporaryValues => false;

		public override Guid Next(EntityEntry entry)
		{
			return Guid.CreateVersion7();
		}
	}
}