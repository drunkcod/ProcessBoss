using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProcessBoss.JsonRpc
{
	[JsonConverter(typeof(JsonRpcMessageConverter))]
	public class JsonRpcMessage 
	{
	}

	public class JsonRpcMessageConverter : JsonConverter<JsonRpcMessage>
	{
		public override JsonRpcMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			if(reader.TokenType != JsonTokenType.StartObject)
				throw new InvalidOperationException();

			string version = null;
			RequestId id = default;
			string method = null;
			object @params = null;
			object result = default;
			JsonRpcError error = null;

			while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
				if(reader.TokenType == JsonTokenType.PropertyName) {
					var p = reader.GetString();
					reader.Read();
					switch(p) {
						default: reader.Skip(); break;

						case "jsonrpc":
							version = reader.GetString();
							break;
						case "id": 
							id = JsonSerializer.Deserialize<RequestId>(ref reader, options);
							break;
						case "method": 
							method = reader.GetString();
							break;
						case "params":
							@params = JsonSerializer.Deserialize<object>(ref reader, options);
							break;
						case "result":
							result = JsonSerializer.Deserialize<object>(ref reader, options);
							break;
						case "error":
							error = JsonSerializer.Deserialize<JsonRpcError>(ref reader, options);
							break;
					}
				}
			}
			if(id.IsMissing)
				return new JsonRpcNotification {
					Version = version,
					Method = method,
					Parameters = @params,
				};

			if(method != null) 
				return new JsonRpcRequest {
					Id = id,
					Method = method,
					Parameters = @params,
				};

			return new JsonRpcResponse {
				Version = version,
				Id = id,
				Result = result,
				Error = error,
			};
		}

		public override void Write(Utf8JsonWriter writer, JsonRpcMessage value, JsonSerializerOptions options) {
			throw new NotImplementedException();
		}
	}

	public class JsonRpcRequest : JsonRpcMessage
	{
		[JsonPropertyName("jsonrpc")]
		public string Version => "2.0";

		[JsonPropertyName("id")]
		public RequestId Id { get; set; }

		[JsonPropertyName("method")]
		public string Method { get; set; }

		[JsonPropertyName("params")]
		public object Parameters { get; set; }
	}

	public class JsonRpcNotification : JsonRpcMessage
	{
		[JsonPropertyName("jsonrpc")]
		public string Version { get; set; }

		[JsonPropertyName("method")]
		public string Method { get; set; }

		[JsonPropertyName("params")]
		public object Parameters { get; set; }
	}

	public class JsonRpcResponse : JsonRpcMessage, IRpcResponse
	{
		[JsonPropertyName("jsonrpc")]
		public string Version { get; set; }

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

	static class JsonElementExtensions
	{
		public static T ToObject<T>(in this JsonElement json, JsonSerializerOptions options = null) {
			var ms = new MemoryStream();
			using(var writer = new Utf8JsonWriter(ms))
				json.WriteTo(writer);
			ms.TryGetBuffer(out var buffer);
			return JsonSerializer.Deserialize<T>(buffer, options);
		}
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
