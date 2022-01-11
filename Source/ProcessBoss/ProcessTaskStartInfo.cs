using System.Diagnostics;
using System.Text;

namespace ProcessBoss
{
	public class ProcessTaskStartInfo
	{
		public string? WorkingDirectory;
		public string? FileName;
		public string? Arguments;
		public Encoding? Encoding;

		internal ProcessStartInfo ToProcessStartInfo() {
			var si = new ProcessStartInfo {
				WorkingDirectory = WorkingDirectory,
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
}
