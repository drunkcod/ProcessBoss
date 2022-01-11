using System.IO;
using System.Text;

namespace ProcessBoss
{
	public class ProcessTaskResult
	{
		public int ExitCode;
		public readonly MemoryStream Output;
		public readonly Encoding OutputEncoding;
		public readonly MemoryStream Error;	
		public readonly Encoding ErrorEncoding;

		public ProcessTaskResult(MemoryStream output, Encoding outputEncoding, MemoryStream error, Encoding errorEncoding) {
			this.Output = output;
			this.OutputEncoding = outputEncoding;
			this.Error = error;
			this.ErrorEncoding = errorEncoding;
		}
	}
}
