using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ProcessBoss
{

	public static class ProcessTask
	{
		class ProcessWaitHandle : WaitHandle
		{
			public ProcessWaitHandle(Process process) {
				this.SafeWaitHandle = new SafeWaitHandle(process.Handle, false);
			}
		}

		class ProcessWait
		{
			readonly Process process;
			readonly IProcessTask processTask;
			RegisteredWaitHandle? handle;

			ProcessWait(Process process, IProcessTask processTask) {
				this.process = process;
				this.processTask = processTask;
			}

			public static void Register(Process process, IProcessTask processTask) {
				var p = new ProcessWait(process, processTask);
				p.handle = ThreadPool.RegisterWaitForSingleObject(
					new ProcessWaitHandle(process),
					HandleWait,
					p, -1,
					executeOnlyOnce: true);
			}

			static void HandleWait(object state, bool timedOut) {
				var x = (ProcessWait)state;
				x.handle?.Unregister(null);
				x.handle = null;
				try {
					x.processTask.SetResult(x.process);
				}
				catch (Exception ex) {
					x.processTask.SetException(ex);
				}
				finally {
					x.process?.Dispose();
				}
			}
		}

		class DefaultProcessTaskState
		{
			public readonly ProcessTaskResult Result;
			public readonly Task IO;

			DefaultProcessTaskState(ProcessTaskResult result, Task io) {
				this.Result = result;
				this.IO = io;
			}
				 
			public static DefaultProcessTaskState Create(Process p) => Create(p, null);
			
			public static DefaultProcessTaskState Create(Process p, Func<StreamWriter, Task>? writeInput) {
				var result = new ProcessTaskResult(
					new MemoryStream(),
					p.StandardOutput.CurrentEncoding,
					new MemoryStream(),
					p.StandardError.CurrentEncoding);;

				return new DefaultProcessTaskState(result, 
					Task.WhenAll(
						HandleInput(p, writeInput),
						p.StandardOutput.BaseStream.CopyToAsync(result.Output),
						p.StandardError.BaseStream.CopyToAsync(result.Error)));
			}

			public static async Task<ProcessTaskResult> GetResult(Process process, DefaultProcessTaskState p) {
				p.Result.ExitCode = process.ExitCode;

				await p.IO.ConfigureAwait(false);
				p.Result.Output.Position = 0;
				p.Result.Error.Position = 0;

				return p.Result;
			}
		}

		interface IProcessTask
		{
			void SetResult(Process process);
			void SetException(Exception exception);
		}

		class ProcessTaskState<TState, TResult> : IProcessTask
		{
			readonly TaskCompletionSource<TResult> tsc = new();
			readonly TState state;
			readonly Func<Process, TState, TResult> getResult;

			public ProcessTaskState(TState state, Func<Process, TState, TResult> getResult) {
				this.state = state;
				this.getResult = getResult;
			}

			public Task<TResult> Task => tsc.Task;

			public void SetResult(Process process) => tsc.SetResult(getResult(process, state));
			public void SetException(Exception ex) => tsc.SetException(ex);
		}

		static async Task HandleInput(Process p, Func<StreamWriter, Task>? writeInput) {
			if(writeInput != null)
				await writeInput(p.StandardInput);
			else
				p.StandardInput.Close();
		}

		static T Id<T>(Process p, T item) => item;

		public static Task<ProcessTaskResult> Start(ProcessTaskStartInfo startInfo) => 
			Start(startInfo, DefaultProcessTaskState.Create, DefaultProcessTaskState.GetResult);
		
		public static Task<ProcessTaskResult> Start(ProcessTaskStartInfo startInfo, Func<StreamWriter, Task> writeInput) => 
			Start(startInfo, x => DefaultProcessTaskState.Create(x, writeInput), DefaultProcessTaskState.GetResult);

		public static Task Start(ProcessTaskStartInfo startInfo, Func<Process, Task> setup) =>
			Start(startInfo, setup, Id);

		public static async Task<TResult> Start<TState, TResult>(ProcessTaskStartInfo startInfo, Func<Process, TState> setup, Func<TState, Task<TResult>> getResult) =>
			await getResult(await Start(startInfo, setup, Id).ConfigureAwait(false)).ConfigureAwait(false);
		
		public static async Task<TResult> Start<TState, TResult>(ProcessTaskStartInfo startInfo, Func<Process, TState> setup, Func<Process, TState, Task<TResult>> getResult) {
			var t = Start<TState, Task<TResult>>(startInfo, setup, getResult);
			return await (await t.ConfigureAwait(false)).ConfigureAwait(false);
		}

		public static Task<TResult> Start<TState, TResult>(ProcessTaskStartInfo startInfo, Func<Process, TState> setup, Func<Process, TState, TResult> getResult) {
			var p = Process.Start(startInfo.ToProcessStartInfo());
			var state = new ProcessTaskState<TState, TResult>(setup(p), getResult);
			ProcessWait.Register(p, state);
			return state.Task;
		}
	}
}
