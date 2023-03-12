namespace MdsCloud.SdkDotNet.Utils;

public interface IAuthManager
{
    Task<string> GetAuthenticationToken();
    Task<string> GetAuthenticationToken(string accountId, string userId, string password);
    void SetAuthenticationToken(string token);
}
