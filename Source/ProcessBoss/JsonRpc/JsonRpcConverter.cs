using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProcessBoss.JsonRpc
{
	public abstract class JsonRpcConverter<T> : JsonConverter<T>
	{
		protected RequestId ReadRequestId(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			reader.TokenType switch
			{
				JsonTokenType.Number => new RequestId(reader.GetInt64()),
				JsonTokenType.String => new RequestId(reader.GetString()),
				JsonTokenType.Null => default,
				_ => throw new InvalidOperationException(),
			};

		protected void WriteRequestId(Utf8JsonWriter writer, RequestId value, JsonSerializerOptions options) {
			if (value.IsNumber)
				writer.WriteNumberValue(value.Number);
			else writer.WriteStringValue(value.String);
		}
	}
}
