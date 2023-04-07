using MdsCloud.Identity.Utils;

namespace MdsCloud.Identity.Test.Unit.Utils;

public class PasswordHasherTests
{
    [Fact]
    public void Hashed_Password_Can_Be_Verified_Against()
    {
        const string samplePass = "pass";
        var result = PasswordHasher.Hash(samplePass);
        var verified = PasswordHasher.Verify(samplePass, result);
        Assert.Equal("$MYHASH$V1$10000$", result.Substring(0, 17));
        Assert.True(verified);
    }
}
