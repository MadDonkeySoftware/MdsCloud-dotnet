using MdsCloud.SdkDotNet.Utils;

namespace MdsCloud.SdkDotNet.Test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var foo = new Utilities();

        Assert.NotNull(foo);
    }
}
