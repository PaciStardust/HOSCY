using HoscyCore.Services.Network;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockWebClient : MockStartStopServiceBase, IWebClient
{
    public string DownloadContents { get; set; } = string.Empty;
    public string SendResultString { get; set; } = string.Empty;
    public byte[] SendResultBytes { get; set; } = [];
    public readonly List<HttpRequestMessage> Requests = [];
    public int ArtificialDelayMs { get; set; } = 0;

    public async Task<Res> DownloadAsync(string _, string fileLocation, int timeoutMs = 5000)
    {
        if (ArtificialDelayMs > timeoutMs)
        {
            await Task.Delay(timeoutMs);
            return ResC.Fail(ResMsg.Err("MockWebClient: Download timed out"));
        }
        await Task.Delay(ArtificialDelayMs);
        File.WriteAllText(fileLocation, DownloadContents);
        return ResC.Ok();
    }

    public async Task<Res<string>> SendAsyncString(HttpRequestMessage requestMessage, int timeoutMs = 5000)
    {
        Requests.Add(requestMessage);
        if (ArtificialDelayMs > timeoutMs)
        {
            await Task.Delay(timeoutMs);
            return ResC.TFail<string>(ResMsg.Err("MockWebClient: Send timed out"));
        }
        await Task.Delay(ArtificialDelayMs);
        return ResC.TOk(SendResultString);
    }

    public async Task<Res<byte[]>> SendAsyncBytes(HttpRequestMessage requestMessage, int timeoutMs = 5000)
    {
        Requests.Add(requestMessage);
        if (ArtificialDelayMs > timeoutMs)
        {
            await Task.Delay(timeoutMs);
            return ResC.TFail<byte[]>(ResMsg.Err("MockWebClient: Send timed out"));
        }
        await Task.Delay(ArtificialDelayMs);
        return ResC.TOk(SendResultBytes);
    }

    public override Res Start()
    {
        Requests.Clear();
        return base.Start();
    }

    public override Res Stop()
    {
        var res = base.Stop();
        Requests.Clear();
        return res;
    }
}