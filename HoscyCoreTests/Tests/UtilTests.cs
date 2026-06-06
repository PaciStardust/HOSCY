#pragma warning disable IDE0130 // Namespace does not match folder structure
using System.Diagnostics;
using HoscyCore.Utility;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests.UtilTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class UtilTests : TestBase<UtilTests>
{
    [Test]
    public void GetActualContentFolder()
    {
        var level1 = Path.Join(_tempFolder, "contentTest1");
        Directory.CreateDirectory(level1);

        var level2 = Path.Join(level1, "contentTest2");
        Directory.CreateDirectory(level2);

        var level3 = Path.Join(level2, "contentTest3");
        Directory.CreateDirectory(level3);

        var level4 = Path.Join(level3, "aba.txt");
        File.WriteAllText(level4, "Test");

        var pathRes = PathUtils.GetActualContentFolder(level1, _logger);
        pathRes.AssertOk();
        Assert.That(pathRes.Value, Is.EqualTo(level3));
    }

    [Test, Explicit]
    public void ProcessStartStop()
    {
        var proc = Process.Start("foot");
        proc.Kill();
        Assert.That(OtherUtils.HasProcessExitedSafe(proc));
    }

    [Test]
    public void ExtractFromJson()
    {
        var res = OtherUtils.ExtractFromJson("test", @"{""test"": ""hello""}", _logger);
        res.AssertOk();
        Assert.That(res.Value, Is.EqualTo("hello"));

        res = OtherUtils.ExtractFromJson("test", @"{""tests"": ""hello""}", _logger);
        res.AssertFail();

        res = OtherUtils.ExtractFromJson("test", string.Empty, _logger);
        res.AssertFail();
    }

    [Test]
    public void WaitWhile()
    {
        var value = false;
        var thread = new Thread(new ThreadStart(() => {Thread.Sleep(100); value = true; }));
        thread.Start();
        var wait = OtherUtils.WaitWhile(() => !value, 200, 10);
        Assert.That(wait);

        value = false;
        thread = new Thread(new ThreadStart(() => {Thread.Sleep(100); value = true; }));
        thread.Start();
        wait = OtherUtils.WaitWhile(() => !value, 50, 10);
        Assert.That(!wait);
    }

    [Test]
    public async Task WaitWhileAsync()
    {
        var value = false;
        var task = Task.Run(async () => { await Task.Delay(100); value = true; });
        var wait = await OtherUtils.WaitWhileAsync(() => !value, 200, 10);
        Assert.That(wait);

        value = false;
        task = Task.Run(async () => { await Task.Delay(100); value = true; });
        wait = await OtherUtils.WaitWhileAsync(() => !value, 50, 10);
        Assert.That(!wait);
    }

    [Test]
    public async Task TrimBySpace()
    {
        var test = "aaaa aa";
        test = OtherUtils.TrimBySpace(test, 5);
        Assert.That(test, Is.EqualTo("aaaa"));

        test = "aaaa\naa";
        test = OtherUtils.TrimBySpace(test, 5);
        Assert.That(test, Is.EqualTo("aaaa"));

        test = "aaaaaaaaa";
        test = OtherUtils.TrimBySpace(test, 5);
        Assert.That(test, Is.EqualTo("aaaaa"));
    }

    [Test]
    public void VersionTest()
    {
        Assert.That(LaunchUtils.GetVersion(), Is.Not.Empty);
        Assert.That(LaunchUtils.GetVersion(), Does.StartWith("v"));
    }
}