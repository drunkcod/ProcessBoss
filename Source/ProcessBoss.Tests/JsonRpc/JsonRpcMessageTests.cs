using System.Text.Json;
using CheckThat;
using ProcessBoss.Rpc;
using Xunit;

namespace ProcessBoss.JsonRpc.Tests
{
	public class JsonRpcFixture
	{
		protected T RoundtripAs<T>(object item) => JsonSerializer.Deserialize<T>(ToJson(item));

		protected string ToJson(object obj) => JsonSerializer.Serialize(obj);

		protected bool HasProperty(object obj, string propertyName) {
			using (var json = JsonDocument.Parse(ToJson(obj)))
				return json.RootElement.TryGetProperty(propertyName, out var _);
		}
	}

	public class JsonRpcFixture<T> : JsonRpcFixture
	{
		protected class SomeThing
		{
			public int Value { get; set; }
		}

		protected T FromJson(string input) => JsonSerializer.Deserialize<T>(input);		
	}

	public class JsonRpcMessageTests : JsonRpcFixture<JsonRpcMessage>
	{
		[Fact]
		public void deserialize_message_type() =>
			Check.That(
				() => FromJson(ToJson(new { method = "notification" })) is JsonRpcRequest,
				() => FromJson(ToJson(new { id = 1, method = "request" })) is JsonRpcRequest,
				() => FromJson(ToJson(new { id = 2, result = "response" })) is JsonRpcResponse,
				() => FromJson(ToJson(new { id = 3, error = new JsonRpcError { Code = -1 } })) is JsonRpcResponse);

		[Fact]
		public void notification() =>
			Check.That(() => HasProperty(new JsonRpcRequest { Method = "notify!" }, "id") == false);

		[Fact]
		public void response_roundtrip() {
			var r = (JsonRpcResponse)Check.That(
				() => FromJson(ToJson(new JsonRpcResponse { Id = 1, Result = new SomeThing { Value = 42 } })) is JsonRpcResponse);
			Check.That(() => r.GetResult<SomeThing>().Value == 42);
		}

		[Fact]
		public void request_as_message() {
			var request = new JsonRpcRequest { Id = "hello", Method = "Log", Parameters = new[] { "Hello", "World", "!" } };
			Check.That(
				() => ToJson(new { r = (JsonRpcMessage)request }) == ToJson(new { r = request }),
				() => ToJson(new { r = (JsonRpcMessage)null }) == ToJson(new { r = (JsonRpcRequest)null}));
		}
	}

	public class JsonRpcResponseTests : JsonRpcFixture<JsonRpcResponse>
	{
		[Fact]
		public void response_roundtrip() => Check
			.With(() => FromJson(ToJson(new JsonRpcResponse { Id = 1, Result = new SomeThing { Value = 42 } })))
			.That(
				x => x.Id == 1,
				x => x.GetResult<SomeThing>().Value == 42);
		
		[Fact]
		public void success_response_must_not_contain_error() =>
			Check.That(() => HasProperty(new JsonRpcResponse { Id = "success", Result = "Ok" }, "error") == false);

		[Fact]
		public void error_response_must_not_contain_result() =>
			Check.That(() => HasProperty(new JsonRpcResponse { Id = "error", Error = new JsonRpcError { Code = -1 } }, "result") == false);

		[Fact]
		public void response_error() {
			var error = new JsonRpcError { Code = -1, Message = "Error Error" };
			var r = FromJson(ToJson(new JsonRpcResponse { Id = 1, Error = error }));

			var ex = (JsonRpcException)Check.That(() => ((IRpcResponse)r).Exception is JsonRpcException);
			Check.That(
				() => ex.Code == error.Code,
				() => ex.Message == error.Message);
		}

	}
}
