namespace MdsCloud.CLI.Stack.Builders;

public interface IWriteConfigsArgs
{
    string IdentityPrivateKeyPassword { get; }
    string LocalSourceRootDirectory { get; }
    Dictionary<string, string> DbConnectionStings { get; }
    Dictionary<string, bool> IsLocalDev { get; }
}
