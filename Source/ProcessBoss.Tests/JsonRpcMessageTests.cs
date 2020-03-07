using System.Text.Json;
using Cone;
using Xunit;

namespace ProcessBoss.JsonRpc.Tests
{
	public class JsonRpcMessageTests
	{
		[Fact]
		public void deserialize_message_type() =>
			Check.That(
				() => FromJson(ToJson(new { method = "notification" })) is JsonRpcNotification,
				() => FromJson(ToJson(new { id = 1, method = "request" })) is JsonRpcRequest,
				() => FromJson(ToJson(new { id = 2, result = "response" })) is JsonRpcResponse,
				() => FromJson(ToJson(new { id = 3, error = new JsonRpcError { Code = -1 } })) is JsonRpcResponse);

		[Fact]
		public void response_roundtrip() {
			var r = (JsonRpcResponse)Check.That(
				() => FromJson(ToJson(new JsonRpcResponse { Id = 1, Result = new SomeThing { Value = 42 } })) is JsonRpcResponse);
			Check.That(() => r.GetResult<SomeThing>().Value == 42);
		}

		[Fact]
		public void response_error() {
			var error = new JsonRpcError { Code = -1, Message = "Error Error" };
			var r = (JsonRpcResponse)FromJson(ToJson(new JsonRpcResponse { Id = 1, Error = error }));

			var ex = (JsonRpcException)Check.That(() => ((IRpcResponse)r).Exception is JsonRpcException);
			Check.That(
				() => ex.Code == error.Code,
				() => ex.Message == error.Message);
		}

		class SomeThing
		{
			public int Value { get; set; }
		}

		JsonRpcMessage FromJson(string input) => JsonSerializer.Deserialize<JsonRpcMessage>(input); 
		string ToJson(object obj) => JsonSerializer.Serialize(obj);
	}
}
