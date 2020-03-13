using System;

namespace ProcessBoss.Rpc
{
	public interface IRpcResponse
	{
		RequestId Id { get; }
		Exception Exception { get; }
		T GetResult<T>();
	}

	public class RpcResponse : IRpcResponse
	{
		readonly object result;

		public RequestId Id { get; }
		public Exception Exception { get; }

		RpcResponse(RequestId id, object result, Exception exception) {
			this.Id = id;
			this.result = result;
			this.Exception = exception;
		}

		public static RpcResponse Result(RequestId id, object result) => new RpcResponse(id, result, null);
		public static RpcResponse Error(RequestId id, Exception exception) => new RpcResponse(id, null, exception);

		T IRpcResponse.GetResult<T>() => (T)result;
	}
}
