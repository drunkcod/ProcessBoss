// See https://aka.ms/new-console-template for more information

using DataBoss;

var options = PowerArgs.Parse(args).Into<Args>();

if(options.Output != null)
	Console.Write(options.Output);

if(options.Error != null)
	Console.Error.Write(options.Error);

return options.ExitCode;

class Args
{
	public int ExitCode;
	public string? Output;
	public string? Error;
}