using Identity.Utils;

namespace Identity.Test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        const string samplePass = "pass";
        var result = PasswordHasher.Hash(samplePass);
        var verified = PasswordHasher.Verify(samplePass, result);
        Assert.Equal("$MYHASH$V1$10000$", result.Substring(0, 17));
        Assert.True(verified);
    }
}
