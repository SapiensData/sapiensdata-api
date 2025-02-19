using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SapiensDataAPI.Services.ByteArrayPlainTextConverter
{
	public class ByteArrayPlainTextConverterService : JsonConverter<byte[]>
	{
		public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			// Read the plaintext string from JSON
			string? plainText = reader.GetString();

			// Convert plaintext to byte array (UTF-8 encoding)
			return plainText is not null ? Encoding.UTF8.GetBytes(plainText) : [];
		}

		public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
		{
			// Convert byte array back to a plaintext string (UTF-8 decoding)
			writer.WriteStringValue(Encoding.UTF8.GetString(value));
		}
	}
}