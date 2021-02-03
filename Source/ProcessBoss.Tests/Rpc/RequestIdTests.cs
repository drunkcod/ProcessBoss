using System.Text.Json;
using Cone;
using ProcessBoss.Rpc;
using Xunit;

namespace ProcessBoss.JsonRpc.Tests
{
	public class RequestIdSpec
	{
		[Fact]
		public void default_is_missing() {
			Check.That(() => default(RequestId).IsMissing);
		}

		[Fact]
		public void number() {
			Check.With(() => new RequestId(123L)).That(
				id => !id.IsMissing,
				id => id.Number == 123,
				id => id.String == null);
		}

		[Fact]
		public void @string() {
			Check.With(() => new RequestId("Hello.")).That(
				id => !id.IsMissing,
				id => id.String == "Hello.",
				id => id.Number == 0);
		}

		[Fact]
		public void equatable() {
			Check.That(
				() => new RequestId(1).Equals(new RequestId(1)),
				() => new RequestId("One").Equals(new RequestId(1)) == false,
				() => new RequestId(1).Equals(new RequestId()) == false,
				() => new RequestId("Str").Equals(new RequestId("Str")));
		}

		[Fact]
		public void json() 
		{
			Check.That(
				() => ToJson(new { id = default(RequestId) }) == ToJson(new { id = (string)null }),
				() => ToJson(new { id = new RequestId(42) }) == ToJson(new { id = 42 }),
				() => ToJson(new { id = new RequestId("Foo") }) == ToJson(new { id = "Foo" }),
				() => FromJson(new MyEvent { Id = new RequestId(null) }).Id == new RequestId(),
				() => FromJson(new MyEvent { Id = new RequestId(3) }).Id == new RequestId(3),
				() => FromJson(new MyEvent { Id = new RequestId("bar") }).Id == new RequestId("bar"));
		}

		[Fact]
		public void null_is_not_missing() => 
			Check.That(() => default(RequestId) != RequestId.Null);

		class MyEvent 
		{ 
			public RequestId Id { get; set; }
		};

		static string ToJson<T>(T value) => JsonSerializer.Serialize(value);
		static T FromJson<T>(T value) => JsonSerializer.Deserialize<T>(ToJson(value));
	}
}
