using System.Threading.Tasks;
using CheckThat;
using Xunit;

namespace ProcessBoss
{
	public class ProcessTask_
	{
		[Fact]
		public async Task await_success() {
			var r = await ProcessTask.Start(new ProcessTaskStartInfo { 
				FileName = "dotnet",
				Arguments = "SimpleApp.dll"
			});
			Check.That(() => r.ExitCode == 0);
		}

		[Fact]
		public async Task reads_output() {
			var r = await ProcessTask.Start(new ProcessTaskStartInfo {
				FileName = "dotnet",
				Arguments = "SimpleApp.dll -Output \"Hello World.\""
			});
			Check.That(() => r.OutputEncoding.GetString(r.Output.ToArray()) == "Hello World.");
		}

		[Fact]
		public async Task reads_output_transformed() {
			var r = await ProcessTask.Start(new ProcessTaskStartInfo {
				FileName = "dotnet",
				Arguments = "SimpleApp.dll -Output \"HELLO WORLD\""
			}, 
			p => Task.Run(() => p.StandardOutput.ReadToEnd()), 
			async (p, x) => (await x).ToLower());
			Check.That(() => r == "hello world");
		}

		[Fact]
		public async Task reads_Error_stream() {
			var r = await ProcessTask.Start(new ProcessTaskStartInfo {
				FileName = "dotnet",
				Arguments = "SimpleApp.dll -Error \"Hello ERROR World.\""
			});
			Check.That(() => r.ErrorEncoding.GetString(r.Error.ToArray()) == "Hello ERROR World.");
		}
	}
}
