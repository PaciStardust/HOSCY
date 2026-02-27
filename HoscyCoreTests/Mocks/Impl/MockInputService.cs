using HoscyCore.Services.Input;
using HoscyCore.Services.Output.Core;

namespace HoscyCoreTests.Mocks.Impl;

public class MockInputService : IInputService
{
    public readonly List<string> AudioMessages = [];
    public readonly List<string> OtherMessages = [];
    public readonly List<string> TextMessages = [];
    public readonly List<string> ManualMessages = [];
    public readonly List<(string, OutputNotificationPriority)> TextNotification = [];

    public void SendExternalAudioMessage(string contents)
    {
        AudioMessages.Add(contents);
    }

    public void SendExternalOtherMessage(string contents)
    {
        OtherMessages.Add(contents);
    }

    public void SendExternalTextMessage(string contents)
    {
        TextMessages.Add(contents);
    }

    public void SendExternalTextNotification(string contents, OutputNotificationPriority prio = OutputNotificationPriority.Medium)
    {
        TextNotification.Add((contents, prio));
    }

    public void SendManualMessage(string contents)
    {
        ManualMessages.Add(contents);
    }

    public void Clear()
    {
        AudioMessages.Clear();
        OtherMessages.Clear();
        TextMessages.Clear();
        ManualMessages.Clear();
        TextNotification.Clear();
    }
}