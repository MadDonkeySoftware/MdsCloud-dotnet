namespace MdsCloud.CLI.Utils;

public static class DirectoryTools
{
    public static void EnsurePathsExists(params string[] paths)
    {
        foreach (var path in paths)
        {
            EnsurePathExists(path);
        }
    }

    public static void EnsurePathExists(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            EnsurePathPartExists("/", parts.Skip(1).ToArray());
            return;
        }

        throw new NotSupportedException();
    }

    private static void EnsurePathPartExists(string root, IReadOnlyList<string> nodes)
    {
        if (nodes.Count == 0)
            return;

        var newPath = Path.Join(root, nodes[0]);
        if (!Directory.Exists(newPath))
        {
            Directory.CreateDirectory(newPath);
        }

        EnsurePathPartExists(newPath, nodes.Skip(1).ToArray());
    }
}
