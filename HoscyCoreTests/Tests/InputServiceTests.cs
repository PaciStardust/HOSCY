using System.Text;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Input;
using HoscyCore.Services.Output.Core;
using HoscyCoreTests.Mocks.Impl;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.InputServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class InputServiceFunctionTests : TestBase<InputServiceFunctionTests>
{
    private InputService _input = null!;
    private readonly ConfigModel _config = new();
    private readonly MockOutputManagerService _output = new();

    protected override void OneTimeSetupExtra()
    {
        _input = new InputService(_config, _output, _logger);
    }

    protected override void SetupExtra()
    {
        _output.Clear();

        _config.ManualInput_DoPreprocessFull = false;
        _config.ManualInput_DoPreprocessPartial = false;
        _config.ManualInput_DoTranslate = false;
        _config.ManualInput_SendViaAudio = false;
        _config.ManualInput_SendViaOther = false;
        _config.ManualInput_SendViaText = false;

        _config.ExternalInput_DoPreprocessFull = false;
        _config.ExternalInput_DoPreprocessPartial = false;
        _config.ExternalInput_DoTranslate = false;
    }

    [Test]
    public void ExternalSendTest()
    {
        const string AUDIO = "Audio";
        const string OTHER = "Other";
        const string TEXT_M = "TxM";
        const string TEXT_N = "TxN";

        var random = new Random();
        var prios = Enum.GetValues<OutputNotificationPriority>();
        for (var i = 0; i < 512; i++)
        {
            var flag = random.Next(0b1000); //Flags
            _config.ExternalInput_DoPreprocessFull = (flag & 0b0100) != 0;
            _config.ExternalInput_DoPreprocessPartial = (flag & 0b0010) != 0;
            _config.ExternalInput_DoTranslate = (flag & 0b0001) != 0;
            
            var offset = i * 3;
            var prio = prios[random.Next(prios.Length)];
            var expectedOutput = (_config.ExternalInput_DoPreprocessFull ? OutputSettingsFlags.DoPreprocessFull : OutputSettingsFlags.None)
                | (_config.ExternalInput_DoPreprocessPartial ? OutputSettingsFlags.DoPreprocessPartial : OutputSettingsFlags.None)
                | (_config.ExternalInput_DoTranslate ? OutputSettingsFlags.DoTranslate : OutputSettingsFlags.None);

            _input.SendExternalAudioMessage(AUDIO);
            _input.SendExternalOtherMessage(OTHER);
            _input.SendExternalTextMessage(TEXT_M);
            _input.SendExternalTextNotification(TEXT_N, prio);


            using (Assert.EnterMultipleScope())
            {
                Assert.That(_output.Messages, Has.Count.EqualTo(offset + 3), $"{i}: Not correct message count");
                Assert.That(_output.Notifications, Has.Count.EqualTo(i+1), $"{i}: Not correct notification count");
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_output.Messages[offset].Message, Is.EqualTo(AUDIO), $"{i}: Audio message wrong");
                Assert.That(_output.Messages[offset].Flags, Is.EqualTo(expectedOutput | OutputSettingsFlags.AllowAudioOutput), $"{i}: Audio flags wrong");

                Assert.That(_output.Messages[offset + 1].Message, Is.EqualTo(OTHER), $"{i}: Other message wrong");
                Assert.That(_output.Messages[offset + 1].Flags, Is.EqualTo(expectedOutput | OutputSettingsFlags.AllowOtherOutput), $"{i}: Other flags wrong");

                Assert.That(_output.Messages[offset + 2].Message, Is.EqualTo(TEXT_M), $"{i}: Text message wrong");
                Assert.That(_output.Messages[offset + 2].Flags, Is.EqualTo(expectedOutput | OutputSettingsFlags.AllowTextOutput), $"{i}: Text flags wrong");

                Assert.That(_output.Notifications[i].Message, Is.EqualTo(TEXT_N), $"{i}: Notification message wrong");
                Assert.That(_output.Notifications[i].Flags, Is.EqualTo(expectedOutput | OutputSettingsFlags.AllowTextOutput), $"{i}: Notification flags wrong");
                Assert.That(_output.Notifications[i].Prio, Is.EqualTo(prio), $"{i}: Notification prio wrong");
            };
        }
    }

    [Test]
    public void ManualSendTest()
    {
        var random = new Random();
        for (var i = 0; i < 4096; i++)
        {
            var flag = random.Next(0b1000000); //Flags
            _config.ManualInput_DoPreprocessFull = (flag & 0b0100000) != 0;
            _config.ManualInput_DoPreprocessPartial = (flag & 0b0010000) != 0;
            _config.ManualInput_DoTranslate = (flag & 0b0001000) != 0;
            _config.ManualInput_SendViaAudio = (flag & 0b0000100) != 0;
            _config.ManualInput_SendViaOther = (flag & 0b0000010) != 0;
            _config.ManualInput_SendViaText = (flag & 0b0000001) != 0;

            var expectedOutput = (_config.ManualInput_DoPreprocessFull ? OutputSettingsFlags.DoPreprocessFull : OutputSettingsFlags.None)
                | (_config.ManualInput_DoPreprocessPartial ? OutputSettingsFlags.DoPreprocessPartial : OutputSettingsFlags.None)
                | (_config.ManualInput_DoTranslate ? OutputSettingsFlags.DoTranslate : OutputSettingsFlags.None)
                | (_config.ManualInput_SendViaAudio ? OutputSettingsFlags.AllowAudioOutput : OutputSettingsFlags.None)
                | (_config.ManualInput_SendViaOther ? OutputSettingsFlags.AllowOtherOutput : OutputSettingsFlags.None)
                | (_config.ManualInput_SendViaText ? OutputSettingsFlags.AllowTextOutput : OutputSettingsFlags.None);

            var buffer = new byte[16];
            random.NextBytes(buffer);
            var message = Encoding.UTF8.GetString(buffer);
            _input.SendManualMessage(message);

            Assert.That(_output.Messages, Has.Count.EqualTo(i+1), $"{i}: Not correct message count");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_output.Messages[i].Message, Is.EqualTo(message), $"{i}: Message wrong");
                Assert.That(_output.Messages[i].Flags, Is.EqualTo(expectedOutput), $"{i}: Message flags wrong");
            };
        }
    }

    [Test]
    public void EmptyTest()
    {
        _input.SendExternalAudioMessage(string.Empty);
        _input.SendExternalOtherMessage(string.Empty);
        _input.SendExternalTextMessage(string.Empty);
        _input.SendExternalTextNotification(string.Empty);
        _input.SendManualMessage(string.Empty);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_output.Messages, Is.Empty, "Messages should be empty");
            Assert.That(_output.Notifications, Is.Empty, "Notifications should be empty");
        };
    }
}