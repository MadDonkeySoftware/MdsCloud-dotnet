using System.Reflection;

namespace MdsCloud.CLI.Utils;

public static class EmbeddedResources
{
    public static string ReadEmbeddedResource(string path)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);

        if (stream == null)
            throw new Exception($"Embedded resource missing: {path}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
