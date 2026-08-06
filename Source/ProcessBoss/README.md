# ProcessBoss

Your Processes. Like a Boss.

A small async-friendly wrapper around `System.Diagnostics.Process` for starting
external processes, capturing their stdout/stderr, and awaiting their exit
without blocking a thread.

## Usage

```csharp
using ProcessBoss;

var result = await ProcessTask.Start(new ProcessTaskStartInfo {
	FileName = "dotnet",
	Arguments = "--version",
});

Console.WriteLine(result.ExitCode);
```

`ProcessTaskStartInfo` also supports setting `WorkingDirectory`, `Encoding`,
and `EnvironmentVariables` for the child process.
