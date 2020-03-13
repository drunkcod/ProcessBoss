using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessBoss.Rpc;

namespace ProcessBoss.JsonRpc
{
	[JsonConverter(typeof(JsonRpcRequestConveter))]
	public class JsonRpcRequest : JsonRpcMessage
	{
		[JsonPropertyName("id")]
		public RequestId Id { get; set; }

		[JsonPropertyName("method")]
		public string Method { get; set; }

		[JsonPropertyName("params")]
		public object Parameters { get; set; }

		public bool IsNotification => Id.IsMissing;
	}

	public class JsonRpcRequestConveter : JsonRpcConverter<JsonRpcRequest>
	{
		public override JsonRpcRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException();
			var r = new JsonRpcRequest();
			while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
				if(ReadTo(ref reader, Columns.Version))
					r.Version = ReadVersion(ref reader);
				else if(ReadTo(ref reader, Columns.Id))
					r.Id = ReadRequestId(ref reader);
				else if(ReadTo(ref reader, Columns.Method))
					r.Method = reader.GetString();
				else if(ReadTo(ref reader, Columns.Parameters))
					r.Parameters = JsonSerializer.Deserialize<object>(ref reader);
				else 
					reader.Skip();
			}

			return r;
		}

		public override void Write(Utf8JsonWriter writer, JsonRpcRequest value, JsonSerializerOptions options) {
			writer.WriteStartObject();

			writer.WriteString(Columns.Version, value.Version);

			if(!value.Id.IsMissing) {
				writer.WritePropertyName(Columns.Id);
				WriteRequestId(writer, value.Id);
			}

			writer.WriteString(Columns.Method, value.Method);

			writer.WritePropertyName(Columns.Parameters);
			JsonSerializer.Serialize(writer, value.Parameters, options);

			writer.WriteEndObject();
		}
	}
}
