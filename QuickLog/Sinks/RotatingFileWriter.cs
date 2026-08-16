using System.Text;
using System.IO.Compression;
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
        else
            Prune();
    }

    public void WriteLine(string line)
    {
        var bytes = Encoding.UTF8.GetBytes(line + Environment.NewLine);
        WriteBytes(bytes);
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (_options is { MaxFileBytes: > 0 } &&
            _stream.Length > 0 &&
            _stream.Length + bytes.Length > _options.MaxFileBytes)
        {
            Rotate();
        }

        _stream.Write(bytes);
        if (_options?.MaxTotalBytes > 0)
            Prune();
    }

    public void Flush() => _stream.Flush();

    public void Dispose() => _stream.Dispose();

    private FileStream OpenAppend() =>
        new(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

    private void Rotate()
    {
        _stream.Flush();
        _stream.Dispose();

        try
        {
            if (File.Exists(_path))
            {
                var directory = Path.GetDirectoryName(_path) ?? ".";
                var name = Path.GetFileNameWithoutExtension(_path);
                var extension = Path.GetExtension(_path);
                var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fffffff");
                var rotated = GetUniqueRotationPath(directory, name, extension, stamp);
                File.Move(_path, rotated);
                if (_options?.CompressRotatedFiles == true)
                    Compress(rotated);
            }
        }
        finally
        {
            _stream = OpenAppend();
        }

        Prune();
    }

    private void Prune()
    {
        if (_options is null)
            return;

        var directory = Path.GetDirectoryName(_path) ?? ".";
        var name = Path.GetFileNameWithoutExtension(_path);
        var extension = Path.GetExtension(_path);
        var rotations = Directory.EnumerateFiles(directory, $"{name}.*")
            .Where(path => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                           || path.EndsWith(extension + ".gz", StringComparison.OrdinalIgnoreCase))
            .Where(path => !string.Equals(Path.GetFullPath(path), Path.GetFullPath(_path), StringComparison.OrdinalIgnoreCase))
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToList();

        if (_options.MaxAge is { } maxAge && maxAge >= TimeSpan.Zero)
        {
            var cutoff = DateTime.UtcNow - maxAge;
            Delete(rotations.Where(file => file.LastWriteTimeUtc < cutoff));
            rotations = rotations.Where(file => file.Exists).ToList();
        }

        var retainedRotationCount = Math.Max(0, _options.MaxFiles - 1);
        Delete(rotations.Skip(retainedRotationCount));
        rotations = rotations.Where(file => file.Exists).ToList();

        if (_options.MaxTotalBytes > 0)
        {
            var totalBytes = _stream.Length + rotations.Sum(file => file.Length);
            foreach (var file in rotations.OrderBy(file => file.LastWriteTimeUtc))
            {
                if (totalBytes <= _options.MaxTotalBytes)
                    break;

                var length = file.Length;
                Delete([file]);
                if (!file.Exists)
                    totalBytes -= length;
            }
        }
    }

    private static string GetUniqueRotationPath(string directory, string name, string extension, string stamp)
    {
        var path = Path.Combine(directory, $"{name}.{stamp}{extension}");
        for (var suffix = 1; File.Exists(path) || File.Exists(path + ".gz"); suffix++)
            path = Path.Combine(directory, $"{name}.{stamp}.{suffix}{extension}");
        return path;
    }

    private static void Compress(string path)
    {
        var compressedPath = path + ".gz";
        using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var output = new FileStream(compressedPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize))
            input.CopyTo(gzip);

        File.Delete(path);
    }

    private static void Delete(IEnumerable<FileInfo> files)
    {
        foreach (var file in files)
        {
            try { file.Delete(); }
            catch { /* best-effort pruning */ }
        }
    }
}
