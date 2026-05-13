using System.Text;
using QuickLog.Core;

namespace QuickLog.Sinks;

internal sealed class RotatingFileWriter : IDisposable
{
    private readonly string _path;
    private readonly LogRotationOptions? _options;
    private FileStream _stream;

    public RotatingFileWriter(string path, LogRotationOptions? options = null)
    {
        _path = path;
        _options = options?.IsEnabled == true ? options : null;

        Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
        _stream = OpenAppend();

        if (_options?.RotateOnStartup == true && _stream.Length > 0)
            Rotate();
    }

    public void WriteLine(string line)
    {
        var bytes = Encoding.UTF8.GetBytes(line + Environment.NewLine);
        WriteBytes(bytes);
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (_options is not null &&
            _stream.Length > 0 &&
            _stream.Length + bytes.Length > _options.MaxFileBytes)
        {
            Rotate();
        }

        _stream.Write(bytes);
    }

    public void Flush() => _stream.Flush();

    public void Dispose() => _stream.Dispose();

    private FileStream OpenAppend() =>
        new(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

    private void Rotate()
    {
        _stream.Flush();
        _stream.Dispose();

        if (File.Exists(_path))
        {
            var directory = Path.GetDirectoryName(_path) ?? ".";
            var name = Path.GetFileNameWithoutExtension(_path);
            var extension = Path.GetExtension(_path);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            var rotated = Path.Combine(directory, $"{name}.{stamp}{extension}");
            File.Move(_path, rotated, overwrite: true);
        }

        _stream = OpenAppend();
        Prune();
    }

    private void Prune()
    {
        if (_options is null)
            return;

        var directory = Path.GetDirectoryName(_path) ?? ".";
        var name = Path.GetFileNameWithoutExtension(_path);
        var extension = Path.GetExtension(_path);
        var max = Math.Max(1, _options.MaxFiles);

        var files = Directory.GetFiles(directory, $"{name}*{extension}")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Skip(max)
            .ToList();

        foreach (var file in files)
        {
            try { file.Delete(); }
            catch { /* best-effort pruning */ }
        }
    }
}
