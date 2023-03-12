namespace MdsCloud.SdkDotNet.Utils.Cache;

public interface ITokenCache
{
    void Set(string key, string value);
    string? Get(string key);
    void Remove(string key);
    void RemoveAll();
}
