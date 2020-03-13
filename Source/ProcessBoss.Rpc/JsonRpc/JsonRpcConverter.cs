using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessBoss.Rpc;

namespace ProcessBoss.JsonRpc
{
	public abstract class JsonRpcConverter<T> : JsonConverter<T>
	{
		protected class JsonRpcMessageColumns
		{
			static JsonRpcMessageColumns instance;

			JsonRpcMessageColumns() { }

			public readonly JsonEncodedText Version = JsonEncodedText.Encode("jsonrpc");
			public readonly JsonEncodedText Id = JsonEncodedText.Encode("id");
			public readonly JsonEncodedText Method = JsonEncodedText.Encode("method");
			public readonly JsonEncodedText Parameters = JsonEncodedText.Encode("params");
			public readonly JsonEncodedText Result = JsonEncodedText.Encode("result");
			public readonly JsonEncodedText Error = JsonEncodedText.Encode("error");

			public static JsonRpcMessageColumns Instance => instance ?? (instance = new JsonRpcMessageColumns());
		}

		protected JsonRpcMessageColumns Columns => JsonRpcMessageColumns.Instance;

		protected RequestId ReadRequestId(ref Utf8JsonReader reader) =>
			reader.TokenType switch
			{
				JsonTokenType.Number => new RequestId(reader.GetInt64()),
				JsonTokenType.String => new RequestId(reader.GetString()),
				JsonTokenType.Null => default,
				_ => throw new InvalidOperationException(),
			};

		protected string ReadVersion(ref Utf8JsonReader reader) =>
			reader.ValueTextEquals(JsonRpcMessage.ExpectedVersion.EncodedUtf8Bytes)
			? JsonRpcMessage.ExpectedVersion.ToString()
			: reader.GetString();

		protected void WriteRequestId(Utf8JsonWriter writer, RequestId value) {
			if (value.IsNumber)
				writer.WriteNumberValue(value.Number);
			else writer.WriteStringValue(value.String);
		}

		protected void ReadResponseProperties(ref Utf8JsonReader reader, JsonRpcResponse r, JsonSerializerOptions options) {
			do {
				if(ReadTo(ref reader, Columns.Id)) {
					r.Id = ReadRequestId(ref reader);
				} else if (ReadTo(ref reader, Columns.Result))
					r.Result = JsonSerializer.Deserialize<object>(ref reader, options);
				else if (ReadTo(ref reader, Columns.Error))
					r.Error = JsonSerializer.Deserialize<JsonRpcError>(ref reader, options);
				else if (ReadTo(ref reader, Columns.Version))
					r.Version = ReadVersion(ref reader);
				else
					reader.Skip();
			} while (reader.Read() && reader.TokenType != JsonTokenType.EndObject);
		}

		protected static bool IsAt(ref Utf8JsonReader reader, JsonEncodedText propertyName) =>
			reader.ValueTextEquals(propertyName.EncodedUtf8Bytes);

		protected static bool ReadTo(ref Utf8JsonReader reader, JsonEncodedText propertyName) =>
			IsAt(ref reader, propertyName) && reader.Read();

	}
}
