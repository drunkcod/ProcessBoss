using System;
using System.Buffers;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProcessBoss.JsonRpc
{
	[JsonConverter(typeof(JsonRpcMessageConverter))]
	public class JsonRpcMessage 
	{
		protected JsonRpcMessage() { }

		[JsonPropertyName("jsonrpc")]
		public string Version { get; set; } = "2.0";
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
					Version = version,
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

		public override void Write(Utf8JsonWriter writer, JsonRpcMessage value, JsonSerializerOptions options) =>
			JsonSerializer.Serialize(writer, value, value.GetType(), options);
	}

	public class JsonRpcRequest : JsonRpcMessage
	{
		[JsonPropertyName("id")]
		public RequestId Id { get; set; }

		[JsonPropertyName("method")]
		public string Method { get; set; }

		[JsonPropertyName("params")]
		public object Parameters { get; set; }
	}

	public class JsonRpcNotification : JsonRpcMessage
	{
		[JsonPropertyName("method")]
		public string Method { get; set; }

		[JsonPropertyName("params")]
		public object Parameters { get; set; }
	}

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

	class JsonRpcMessageColumns
	{
		static JsonRpcMessageColumns instance;
		
		JsonRpcMessageColumns() { }

		public readonly JsonEncodedText Version = JsonEncodedText.Encode("jsonrpc");
		public readonly JsonEncodedText Id = JsonEncodedText.Encode("id");
		public readonly JsonEncodedText Result = JsonEncodedText.Encode("result");
		public readonly JsonEncodedText Error = JsonEncodedText.Encode("error");

		public static JsonRpcMessageColumns Instance => instance ?? (instance = new JsonRpcMessageColumns());
	}

	public class JsonRpcResponseConverter : JsonRpcConverter<JsonRpcResponse>
	{
		JsonRpcMessageColumns columns => JsonRpcMessageColumns.Instance;

		public override JsonRpcResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			if(reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException();

			var r = new JsonRpcResponse();
			while(reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
				if(reader.ValueTextEquals(columns.Id.EncodedUtf8Bytes)) {
					reader.Read();
					r.Id = ReadRequestId(ref reader, typeof(RequestId), options);
				}
				else if(reader.ValueTextEquals(columns.Result.EncodedUtf8Bytes))
					r.Result = JsonSerializer.Deserialize<object>(ref reader, options);
				else if(reader.ValueTextEquals(columns.Error.EncodedUtf8Bytes))
					r.Error = JsonSerializer.Deserialize<JsonRpcError>(ref reader, options);
				else if(reader.ValueTextEquals(columns.Version.EncodedUtf8Bytes))
					r.Version = reader.GetString();
				else
					reader.Skip();
			}

			return r;
		}

		public override void Write(Utf8JsonWriter writer, JsonRpcResponse value, JsonSerializerOptions options) {
			writer.WriteStartObject();
			
			writer.WriteString(columns.Version, value.Version);			
			
			writer.WritePropertyName(columns.Id);
			WriteRequestId(writer, value.Id, options);
			
			if(value.Result != null) {
				writer.WritePropertyName(columns.Result);
				JsonSerializer.Serialize(writer, value.Result, options);
			}

			if(value.Error != null) {
				writer.WritePropertyName(columns.Error);
				JsonSerializer.Serialize(writer, value.Error, options);
			}
			
			writer.WriteEndObject();
		}
	}

	static class JsonElementExtensions
	{
		class ArrayBufferWriter<T> : IBufferWriter<T>
		{
			const int MinBuffer = 256;
			T[] buffer = new T[MinBuffer];
			int pos;

			public void Advance(int count) {
				pos += count;
			}

			public Memory<T> GetMemory(int sizeHint = 0) {
				EnsureSpace(sizeHint);
				return new Memory<T>(buffer, pos, sizeHint);
			}

			public Span<T> GetSpan(int sizeHint = 0) {
				EnsureSpace(sizeHint);
				return new Span<T>(buffer, pos, sizeHint);
			}

			void EnsureSpace(int sizeHint) {
				if(buffer.Length - pos < sizeHint)
					Array.Resize(ref buffer, buffer.Length + sizeHint);
			}

			public ReadOnlySpan<T> AsReadonlySpan() => new ReadOnlySpan<T>(buffer, 0, pos);
		}

		public static T ToObject<T>(in this JsonElement json, JsonSerializerOptions options = null) {
			var bytes = new ArrayBufferWriter<byte>();
			using(var writer = new Utf8JsonWriter(bytes))
				json.WriteTo(writer);

			return JsonSerializer.Deserialize<T>(bytes.AsReadonlySpan(), options);
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
