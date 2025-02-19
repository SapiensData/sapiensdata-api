using System.Text.Json;

namespace SapiensDataAPI.Services.ByteArrayPlainTextConverter
{
	public interface IByteArrayPlainTextConverterService
	{
		public byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);
		public void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options);
	}
}
