using System;
using Cone;
using Xunit;

namespace ProcessBoss.Rpc.Tests
{
	public class DispatchMapTests
	{
		RequestDispatchMap map = new RequestDispatchMap();

		[Fact]
		public void request_response() { 
			var id = new RequestId(1);
			var result = map.RegisterRequest<string>(id);
			map.OnResponse(RpcResponse.Result(id, "Hello World!"));

			Check.That(() => result.Result == "Hello World!");
		}

		[Fact]
		public void error_response() {
			var id = new RequestId("error");
			var result = map.RegisterRequest<string>(id);
			map.OnResponse(RpcResponse.Error(id, new InvalidOperationException("Something something.")));

			Check.That(() => result.IsFaulted);
		}

		[Fact]
		public void invalid_response_type() {
			var id = new RequestId(2);
			var result = map.RegisterRequest<int>(id);
			map.OnResponse(RpcResponse.Result(id, "Wrong."));

			Check.That(() => result.IsFaulted);
		}

		[Fact]
		public void disallow_duplicate_requests() {
			var id = new RequestId(3);
			map.RegisterRequest<string>(id);
			Check.Exception<InvalidOperationException>(() => map.RegisterRequest<string>(id));
		}
	}
}
