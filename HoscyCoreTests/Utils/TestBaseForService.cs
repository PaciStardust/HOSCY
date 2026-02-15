namespace HoscyCoreTests.Utils;

public abstract class TestBaseForService<T> : TestBase<T>
{
    [Test]
    public void OnlyForStartStop() {}
}