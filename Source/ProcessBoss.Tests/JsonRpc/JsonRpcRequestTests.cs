using Cone;
using Xunit;

namespace ProcessBoss.JsonRpc.Tests
{
	public class JsonRpcRequestTests : JsonRpcFixture<JsonRpcRequest>
	{
		[Fact]
		public void request_roundtrip() => Check
			.With(() => FromJson(ToJson(new JsonRpcRequest { Id = 1, Method = "MyRequest", })))
			.That(
				x => x.Id == 1,
				x => x.Method == "MyRequest");

		[Fact]
		public void parameter_format() =>
			Check.That(
				() => RoundtripAs<JsonRpcRequest>(new { @params = new[] { 1, 2, 3 } }).HasPositionalParameters,
				() => RoundtripAs<JsonRpcRequest>(new { @params = new { Id = 42 } }).HasPositionalParameters == false);
	}
}
