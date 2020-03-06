using System;
using System.Collections.Generic;
using System.Text.Json;
using Cone;
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

		class MyEvent 
		{ 
			public RequestId Id { get; set; }
		};

		static string ToJson<T>(T value) => JsonSerializer.Serialize(value);
		static T FromJson<T>(T value) => JsonSerializer.Deserialize<T>(ToJson(value));
	}

	public class DispatchMapTests
	{
		[Fact]
		public void request_response() { 
			var map = new DispatchMap();

			var id = new RequestId(1);
			var result = (string)null;
			map.RegisterRequest(id, (string x) =>  result = x);
			map.OnResponse(new RpcResponse<string>
			{
				Id = id,
				Result = "Hello World!",
			});

			Check.That(() => result == "Hello World!");
		}
		
		[Fact]
		public void disallow_duplicate_requests() {
			var map = new DispatchMap();
			var id = new RequestId(1);
			map.RegisterRequest<string>(id, Nop);
			Check.Exception<InvalidOperationException>(() => map.RegisterRequest<string>(id, Nop));
		}

		void Nop(string s) { }
	}
}

namespace ProcessBoss.JsonRpc
{
	public interface IRpcResponse 
	{
		RequestId Id { get; }
		T GetResult<T>();
	}

	public class RpcResponse<TResult> : IRpcResponse
	{
		public RequestId Id { get; set; }
		public TResult Result { get; set; }

		T IRpcResponse.GetResult<T>() => (T)(object)Result;
	}


	public class DispatchMap
	{
		readonly Dictionary<RequestId, Action<IRpcResponse>> pendingRequests = new Dictionary<RequestId, Action<IRpcResponse>>();

		public void RegisterRequest<T>(RequestId id, Action<T> onResult) {
			if(!pendingRequests.TryAdd(id,  x => onResult(x.GetResult<T>())))
				throw new InvalidOperationException($"Duplicate request id '{id}'");
		} 

		public void OnResponse(IRpcResponse result) { 
			pendingRequests.TryGetValue(result.Id, out var resultHandler);
			pendingRequests.Remove(result.Id);
			resultHandler(result);
		}
	}
}
