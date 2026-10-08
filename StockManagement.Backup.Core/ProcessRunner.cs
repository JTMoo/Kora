using System.Diagnostics;

namespace StockManagement.Backup.Core;


internal sealed class ProcessRunner : IProcessRunner
{
	public async Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
	{
		var startInfo = new ProcessStartInfo(fileName)
		{
			RedirectStandardError = true,
			RedirectStandardOutput = true,
			UseShellExecute = false
		};
		foreach (var argument in arguments)
			startInfo.ArgumentList.Add(argument);
		foreach (var (key, value) in environment)
			startInfo.Environment[key] = value;

		using var process = new Process { StartInfo = startInfo };
		process.Start();

		var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
		var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
		await process.WaitForExitAsync(cancellationToken);

		return new ProcessResult(process.ExitCode, await stderrTask + await stdoutTask);
	}
}
