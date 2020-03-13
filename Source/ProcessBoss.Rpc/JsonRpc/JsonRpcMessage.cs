using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessBoss.Rpc;

namespace ProcessBoss.JsonRpc
{
	[JsonConverter(typeof(JsonRpcMessageConverter))]
	public class JsonRpcMessage 
	{
		internal static JsonEncodedText ExpectedVersion = JsonEncodedText.Encode("2.0");
		protected JsonRpcMessage() { }

		[JsonPropertyName("jsonrpc")]
		public string Version { get; set; } = ExpectedVersion.ToString();
	}

	public class JsonRpcMessageConverter : JsonRpcConverter<JsonRpcMessage>
	{
		public override JsonRpcMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			if(reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException();

			string version = null;
			RequestId id = default;
			string method = null;
			object @params = null;

			while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
				if(IsAt(ref reader, Columns.Result)
				|| IsAt(ref reader, Columns.Error)) {
					var response = new JsonRpcResponse {
						Version = version,
						Id = id,
					};
					ReadResponseProperties(ref reader, response, options);
					return response;
				} else if(ReadTo(ref reader, Columns.Version)) {
					version = ReadVersion(ref reader);
				} else if(ReadTo(ref reader, Columns.Id)) {
					id = ReadRequestId(ref reader);
				} else if(ReadTo(ref reader, Columns.Method)) {
					method = reader.GetString();
				} else if(ReadTo(ref reader, Columns.Parameters)) {
					@params = JsonSerializer.Deserialize<object>(ref reader, options);
				} else {
					reader.Skip();
				}
			}

			return new JsonRpcRequest {
				Version = version,
				Id = id,
				Method = method,
				Parameters = @params,
			};
		}

		public override void Write(Utf8JsonWriter writer, JsonRpcMessage value, JsonSerializerOptions options) =>
			JsonSerializer.Serialize(writer, value, value.GetType(), options);
	}

	public class JsonRpcError
	{
		[JsonPropertyName("code")]
		public int Code { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; }

		[JsonPropertyName("data")]
		public object Data { get; set; }
	}

	public class JsonRpcException : Exception
	{
		readonly JsonRpcError error;
		public int Code => error.Code;

		public JsonRpcException(JsonRpcError error) : base(error.Message) {
			this.error = error;
		}
	}
}
