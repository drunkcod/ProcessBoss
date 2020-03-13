using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessBoss.Rpc;

namespace ProcessBoss.JsonRpc
{
	[JsonConverter(typeof(JsonRpcResponseConverter))]
	public class JsonRpcResponse : JsonRpcMessage, IRpcResponse
	{
		[JsonPropertyName("id")]
		public RequestId Id { get; set; }

		[JsonPropertyName("result")]
		public object Result { get; set; }

		[JsonPropertyName("error")]
		public JsonRpcError Error { get; set; }

		Exception IRpcResponse.Exception => Error == null ? null : new JsonRpcException(Error);

		public T GetResult<T>() => 
			Result switch {
				T x => x,
				JsonElement json => json.ToObject<T>(),
				_ => (T)Convert.ChangeType(Result, typeof(T))
			};
	}

	public class JsonRpcResponseConverter : JsonRpcConverter<JsonRpcResponse>
	{
		public override JsonRpcResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException();

			var r = new JsonRpcResponse();
			reader.Read();
			ReadResponseProperties(ref reader, r, options);
			return r;
		}

		public override void Write(Utf8JsonWriter writer, JsonRpcResponse value, JsonSerializerOptions options) {
			writer.WriteStartObject();

			writer.WriteString(Columns.Version, value.Version);

			writer.WritePropertyName(Columns.Id);
			WriteRequestId(writer, value.Id);

			if (value.Result != null) {
				writer.WritePropertyName(Columns.Result);
				JsonSerializer.Serialize(writer, value.Result, options);
			}

			if (value.Error != null) {
				writer.WritePropertyName(Columns.Error);
				JsonSerializer.Serialize(writer, value.Error, options);
			}

			writer.WriteEndObject();
		}
	}
}
