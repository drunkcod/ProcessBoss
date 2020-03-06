using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProcessBoss.JsonRpc
{
	public class JsonRpcRequest
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

	public class JsonRpcResponse
	{
		[JsonPropertyName("jsonrpc")]
		public string Version { get; set; }

		[JsonPropertyName("id")]
		public RequestId Id { get; set; }

		[JsonPropertyName("result")]
		public JsonElement Result { get; set; }

		[JsonPropertyName("error")]
		public JsonRpcError Error { get; set; }
	}

	public class JsonRpcError
	{
		[JsonPropertyName("code")]
		public int Code { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; }

		[JsonPropertyName("data")]
		public JsonElement Data { get; set; }
	}
}
