using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ProcessBoss
{
	public class ProcessTaskStartInfo
	{
		public string? WorkingDirectory;
		public required string FileName;
		public string? Arguments;
		public Encoding? Encoding;
		public IDictionary<string, string>? EnvironmentVariables;

		internal ProcessStartInfo ToProcessStartInfo() {
			var si = new ProcessStartInfo(FileName) {
				WorkingDirectory = WorkingDirectory,
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
			if (EnvironmentVariables is not null)
				foreach (var x in EnvironmentVariables)
					si.EnvironmentVariables[x.Key] = x.Value;
			return si;
		}
	}
}
