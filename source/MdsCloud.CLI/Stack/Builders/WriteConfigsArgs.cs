namespace MdsCloud.CLI.Stack.Builders;

public class WriteConfigsArgs : IWriteConfigsArgs
{
    public string IdentityPrivateKeyPassword { get; init; }
    public string LocalSourceRootDirectory { get; init; }
    public Dictionary<string, string> DbConnectionStings { get; } = new();
    public Dictionary<string, bool> IsLocalDev { get; } = new();
}
