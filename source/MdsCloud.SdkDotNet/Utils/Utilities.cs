using MadDonkeySoftware.SystemWrappers.Runtime;

namespace MdsCloud.SdkDotNet.Utils;

internal class Utilities
{
    internal SdkHttpRequestFactory SdkHttpRequestFactory { get; }

    internal IConfigUtilities ConfigUtilities { get; }

    internal IEnvironment Environment { get; }

    internal Utilities()
    {
        SdkHttpRequestFactory = new SdkHttpRequestFactory();
        Environment = new EnvironmentWrapper();
        ConfigUtilities = new ConfigUtilities(Environment);
    }

    internal void VerboseWrite(string message, bool force = false)
    {
        // TODO: move to system wrappers
        if (
            !String.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("MDS_SDK_VERBOSE"))
            || force
        )
        {
            Console.WriteLine(message);
        }
    }
}
