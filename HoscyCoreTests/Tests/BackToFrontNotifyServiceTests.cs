using HoscyCore.Services.Interfacing;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.BackToFrontNotifyServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class BackToFrontNotifyServiceFunctionTests : TestBase<BackToFrontNotifyServiceFunctionTests>
{
    [Test]
    public void NotifyTest()
    {
        var notify = new BackToFrontNotifyService(_logger);

        const string TTL_INFO = "Info";
        const string TTL_ERROR = "Error";
        const string TTL_WARNING = "Warning";
        const string TTL_FATAL = "Fatal";

        const string MSG_ERROR = "Error Text";
        const string MSG_WARNING = "Warning Text";
        const string MSG_FATAL = "Fatal Text";

        List<BackToFrontNotifyEventArgs> notifyArgs = [];

        notify.OnNotificationSent += (sender, args) => { notifyArgs.Add(args); };

        notify.SendInfo(TTL_INFO);
        notify.SendWarning(TTL_WARNING, MSG_WARNING);
        notify.SendError(TTL_ERROR, MSG_ERROR, new Exception(TTL_ERROR));
        notify.SendFatal(TTL_FATAL, MSG_FATAL, new Exception(TTL_FATAL));

        Assert.That(notifyArgs, Has.Count.EqualTo(4));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(notifyArgs[0].Title, Is.EqualTo(TTL_INFO), "Info Title");
            Assert.That(notifyArgs[0].Content, Is.Empty, "Info Message");
            Assert.That(notifyArgs[0].Exception, Is.Null, "Info Ex");
            Assert.That(notifyArgs[0].Level, Is.EqualTo(BackToFrontNotifyLevel.Info), "Info Level");

            Assert.That(notifyArgs[1].Title, Is.EqualTo(TTL_WARNING), "Warning Title");
            Assert.That(notifyArgs[1].Content, Is.EqualTo(MSG_WARNING), "Warning Message");
            Assert.That(notifyArgs[1].Exception, Is.Null, "Warning Ex");
            Assert.That(notifyArgs[1].Level, Is.EqualTo(BackToFrontNotifyLevel.Warning), "Warn Level");

            Assert.That(notifyArgs[2].Title, Is.EqualTo(TTL_ERROR), "Error Title");
            Assert.That(notifyArgs[2].Content, Is.EqualTo(MSG_ERROR), "Error Message");
            Assert.That(notifyArgs[2].Exception?.Message, Is.EqualTo(TTL_ERROR), "Error Ex");
            Assert.That(notifyArgs[2].Level, Is.EqualTo(BackToFrontNotifyLevel.Error), "Error Level");

            Assert.That(notifyArgs[3].Title, Is.EqualTo(TTL_FATAL), "Fatal Title");
            Assert.That(notifyArgs[3].Content, Is.EqualTo(MSG_FATAL), "Fatal Message");
            Assert.That(notifyArgs[3].Exception?.Message, Is.EqualTo(TTL_FATAL), "Fatal Ex");
            Assert.That(notifyArgs[3].Level, Is.EqualTo(BackToFrontNotifyLevel.Fatal), "Fatal Level");
        };
    }
}