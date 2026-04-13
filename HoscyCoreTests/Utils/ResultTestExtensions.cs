using HoscyCore.Utility;

namespace HoscyCoreTests.Utils;

public static class ResultTestExtensions
{
    public static void AssertOk(this ResBase res, string? message = null)
    {
        if (res.IsOk) return;
        Assert.Fail(message ?? $"Result is not okay with message {res.Msg} (expected ok)");
    }

    public static void AssertFail(this ResBase res, string? message = null)
    {
        if (!res.IsOk) return;
        Assert.Fail(message ?? $"Result is okay but expected fail");
    }
}