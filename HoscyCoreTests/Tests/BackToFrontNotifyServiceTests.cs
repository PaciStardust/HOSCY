using HoscyCore.Services.Interfacing;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests;

public class BackToFrontNotifyServiceTests : TestBase<BackToFrontNotifyServiceTests>
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

        var target_info = (string.Empty, string.Empty);
        var target_error = (string.Empty, string.Empty);
        var target_warning = (string.Empty, string.Empty);
        var target_fatal = (string.Empty, string.Empty);

        Exception? target_ex_info = null;
        Exception? target_ex_error = null;
        Exception? target_ex_warning = null;
        Exception? target_ex_fatal = null;

        notify.OnInfo += (sender, args) => { target_info = (args.Title, args.Content); target_ex_info = args.Exception; };
        notify.OnError += (sender, args) => { target_error = (args.Title, args.Content); target_ex_error = args.Exception; };
        notify.OnWarning += (sender, args) => { target_warning = (args.Title, args.Content); target_ex_warning = args.Exception; };
        notify.OnFatal += (sender, args) => { target_fatal = (args.Title, args.Content); target_ex_fatal = args.Exception; };

        notify.SendInfo(TTL_INFO);
        notify.SendWarning(TTL_WARNING, MSG_WARNING);
        notify.SendError(TTL_ERROR, MSG_ERROR, new Exception(TTL_ERROR));
        notify.SendFatal(TTL_FATAL, MSG_FATAL, new Exception(TTL_FATAL));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(target_info.Item1, Is.EqualTo(TTL_INFO), "Info Title");
            Assert.That(target_info.Item2, Is.Empty, "Info Message");
            Assert.That(target_ex_info, Is.Null, "Info Ex");

            Assert.That(target_warning.Item1, Is.EqualTo(TTL_WARNING), "Warning Title");
            Assert.That(target_warning.Item2, Is.EqualTo(MSG_WARNING), "Warning Message");
            Assert.That(target_ex_warning, Is.Null, "Warning Ex");

            Assert.That(target_error.Item1, Is.EqualTo(TTL_ERROR), "Error Title");
            Assert.That(target_error.Item2, Is.EqualTo(MSG_ERROR), "Error Message");
            Assert.That(target_ex_error?.Message, Is.EqualTo(TTL_ERROR), "Error Ex");

            Assert.That(target_fatal.Item1, Is.EqualTo(TTL_FATAL), "Fatal Title");
            Assert.That(target_fatal.Item2, Is.EqualTo(MSG_FATAL), "Fatal Message");
            Assert.That(target_ex_fatal?.Message, Is.EqualTo(TTL_FATAL), "Fatal Ex");
        };
    }
}