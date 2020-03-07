using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessBoss.JsonRpc
{
	public class RequestDispatchMap
	{
		interface IDispatchTask 
		{
			void SetResponse(IRpcResponse response);
		}

		class DispatchTask<T> : IDispatchTask
		{
			readonly TaskCompletionSource<T> tsc = new TaskCompletionSource<T>();

			public void SetResponse(IRpcResponse response) {
				if (response.Exception == null)
					try {
						tsc.SetResult(response.GetResult<T>());
					} catch(Exception ex) {
						tsc.SetException(ex);
					}
				else 
					tsc.SetException(response.Exception);
			}

			public Task<T> Task => tsc.Task;
		}

		readonly Dictionary<RequestId, IDispatchTask> pendingRequests = new Dictionary<RequestId, IDispatchTask>();

		public Task<T> RegisterRequest<T>(RequestId id) {
			try {
				var r = new DispatchTask<T>();
				pendingRequests.Add(id, r);
				return r.Task;
			} catch(ArgumentException) {
				throw new InvalidOperationException($"Duplicate request id '{id}'");
			}
		}

		public void OnResponse(IRpcResponse result) { 
			pendingRequests.TryGetValue(result.Id, out var found);
			pendingRequests.Remove(result.Id);
			found.SetResponse(result);
		}
	}
}
