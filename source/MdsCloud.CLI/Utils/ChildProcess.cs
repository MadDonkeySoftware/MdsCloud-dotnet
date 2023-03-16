using System.Diagnostics;

namespace MdsCloud.CLI.Utils;

public class ChildProcess : IDisposable
{
    public event EventHandler<string>? OnDataOutput;

    private bool _isDisposed;
    private readonly string _fullCommand;
    private readonly string _workingDirectory;
    private readonly string? _logFile;
    private Process? _process;
    private StreamWriter? _outputStream;

    public int ExitCode
    {
        get
        {
            if (_process is not { HasExited: true })
            {
                throw new InvalidOperationException("Process not started or has not exited yet.");
            }
            return _process.ExitCode;
        }
    }

    public ChildProcess(string fullCommand, string workingDirectory, string? logFile = null)
    {
        _fullCommand = fullCommand;
        _workingDirectory = workingDirectory;
        _logFile = logFile;
    }

    public bool Start()
    {
        _process = new Process();

        var parts = _fullCommand.Split(' ');
        _process.StartInfo.FileName = parts[0];
        foreach (var element in parts.Skip(1))
        {
            _process.StartInfo.ArgumentList.Add(element);
        }

        _process.StartInfo.WorkingDirectory = _workingDirectory;

        _process.StartInfo.RedirectStandardError = true;
        _process.StartInfo.RedirectStandardOutput = true;
        _process.StartInfo.RedirectStandardInput = true;

        if (_logFile != null)
        {
            _outputStream = new StreamWriter(_logFile);
            _outputStream.AutoFlush = true;
        }

        _process.OutputDataReceived += (_, args) =>
        {
            _outputStream?.WriteLine(args.Data);
            if (this.OnDataOutput != null && args.Data != null)
            {
                this.OnDataOutput.Invoke(this, args.Data);
            }
        };
        _process.ErrorDataReceived += (_, args) =>
        {
            _outputStream?.WriteLine(args.Data);
            if (this.OnDataOutput != null && args.Data != null)
            {
                this.OnDataOutput.Invoke(this, args.Data);
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        return true;
    }

    // public void Write(string input)
    // {
    //     _process?.StandardInput.WriteLine(input);
    // }

    public void WaitForExit()
    {
        _process?.WaitForExit();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            // Free managed resources
            _process?.Dispose();
            _outputStream?.Dispose();
        }

        _isDisposed = true;
    }
}
