using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ProcessBoss
{
	public class ProcessTaskStartInfo
	{
		public string FileName;
		public string Arguments;
		public Encoding Encoding;

		internal ProcessStartInfo ToProcessStartInfo() {
			var si = new ProcessStartInfo {
				FileName = FileName,
				Arguments = Arguments,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			if (Encoding != null) {
				si.StandardOutputEncoding = Encoding;
				si.StandardErrorEncoding = Encoding;
			}

			return si;
		}
	}

	public class ProcessTaskResult
	{
		public int ExitCode;
		public MemoryStream Output;
		public Encoding OutputEncoding;
		public MemoryStream Error;	
		public Encoding ErrorEncoding;
	}

	public static class ProcessTask
	{
		class ProcessWaitHandle : WaitHandle
		{
			public ProcessWaitHandle(Process process) {
				this.SafeWaitHandle = new SafeWaitHandle(process.Handle, false);
			}
		}

		class ProcessWaitState
		{
			public Process Process;
			public RegisteredWaitHandle Handle;
			public IProcessTask ProcessTask;
		}

		class DefaultProcessTaskState
		{
			public ProcessTaskResult Result;
			public Process Process;
			public Task IO;

			public static DefaultProcessTaskState Create(Process p) => Create(p, null);
			
			public static DefaultProcessTaskState Create(Process p, Func<StreamWriter, Task> writeInput) {
				var result = new ProcessTaskResult {
					Output = new MemoryStream(),
					OutputEncoding = p.StandardOutput.CurrentEncoding,
					Error = new MemoryStream(),
					ErrorEncoding = p.StandardError.CurrentEncoding,
				};

				return new DefaultProcessTaskState {
					Result = result,
					Process = p,
					IO = Task.WhenAll(
						HandleInput(p, writeInput),
						p.StandardOutput.BaseStream.CopyToAsync(result.Output),
						p.StandardError.BaseStream.CopyToAsync(result.Error)),
				};
			}

			public static async Task<ProcessTaskResult> GetResult(DefaultProcessTaskState p) {
				await p.IO.ConfigureAwait(false);
				p.Result.Output.Position = 0;
				p.Result.Error.Position = 0;
				p.Result.ExitCode = p.Process.ExitCode;

				p.Process = null;

				return p.Result;
			}
		}

		interface IProcessTask
		{
			void OnSuccess();
			void OnError(Exception exception);
		}

		class ProcessTaskState<TState, TResult> : IProcessTask
		{
			readonly TaskCompletionSource<TResult> tsc = new TaskCompletionSource<TResult>();
			readonly TState state;
			readonly Func<TState, TResult> getResult;

			public ProcessTaskState(TState state, Func<TState, TResult> getResult) {
				this.state = state;
				this.getResult = getResult;
			}

			public Task<TResult> Task => tsc.Task;

			public void OnSuccess() => tsc.SetResult(getResult(state));
			public void OnError(Exception ex) => tsc.SetException(ex);
		}

		static async Task HandleInput(Process p, Func<StreamWriter, Task> writeInput) {
			if(writeInput != null)
				await writeInput(p.StandardInput);
			else
				p.StandardInput.Close();
		}

		static T Id<T>(T item) => item;

		public static Task<ProcessTaskResult> Start(ProcessTaskStartInfo startInfo) => 
			Start(startInfo.ToProcessStartInfo(), DefaultProcessTaskState.Create, DefaultProcessTaskState.GetResult);
		
		public static Task<ProcessTaskResult> Start(ProcessTaskStartInfo startInfo, Func<StreamWriter, Task> writeInput) => 
			Start(startInfo.ToProcessStartInfo(), x => DefaultProcessTaskState.Create(x, writeInput), DefaultProcessTaskState.GetResult);
		
		public static Task<TResult> Start<TState, TResult>(ProcessTaskStartInfo startInfo, Func<Process, TState> setup, Func<TState, Task<TResult>> getResult) => 
			Start(startInfo.ToProcessStartInfo(), setup, getResult);
		
		public static Task Start(ProcessTaskStartInfo startInfo, Func<Process, Task> setup) =>
			Start(startInfo.ToProcessStartInfo(), setup, Id);

		public static async Task<TResult> Start<TState, TResult>(ProcessStartInfo startInfo, Func<Process, TState> setup, Func<TState, Task<TResult>> getResult) =>
			await getResult(await Start(startInfo, setup, Id).ConfigureAwait(false)).ConfigureAwait(false);
		
		public static Task<TResult> Start<TState, TResult>(ProcessStartInfo startInfo, Func<Process, TState> setup, Func<TState, TResult> getResult) {
			var p = Process.Start(startInfo);
			var state = new ProcessTaskState<TState, TResult>(setup(p), getResult);
			RegisterWait(p, state);
			return state.Task;
		}

		static void RegisterWait(Process p, IProcessTask state) {
			var wait = new ProcessWaitState {
				Process = p,
				ProcessTask = state,
			};
			wait.Handle = ThreadPool.RegisterWaitForSingleObject(
				new ProcessWaitHandle(p),
				HandleWait,
				wait, -1,
				executeOnlyOnce: true);
		}

		static void HandleWait(object state, bool timedOut) {
			var x = (ProcessWaitState)state;
			x.Handle.Unregister(null);
			x.Handle = null;
			try {
				x.ProcessTask.OnSuccess();
			}
			catch (Exception ex) {
				x.ProcessTask.OnError(ex);
			}
			finally {
				x.Process.Dispose();
				x.Process = null;
			}
		}
	}
}
