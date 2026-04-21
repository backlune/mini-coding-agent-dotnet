namespace MiniCodingAgent.Tests.Helpers;

/// <summary>
/// Disposable scratch directory used by every integration-flavoured test.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mca-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(params string[] parts)
    {
        var all = new string[parts.Length + 1];
        all[0] = Path;
        parts.CopyTo(all, 1);
        return System.IO.Path.Combine(all);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch
        {
            // best effort; some files may still be held on Windows
        }
    }
}
