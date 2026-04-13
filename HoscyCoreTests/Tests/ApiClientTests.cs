using System.Text;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Network;
using HoscyCoreTests.Mocks.Impl;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.ApiClientTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class ApiClientFunctionTests : TestBase<ApiClientFunctionTests>
{
    private readonly MockWebClient _webClient = new();
    private ApiClient _apiClient = null!;

    protected override void OneTimeSetupExtra()
    {
        _webClient.Start().AssertOk();
        _apiClient = new ApiClient(_webClient, _logger);
    }

    protected override void SetupExtra()
    {
        _webClient.Start().AssertOk();
        _apiClient.ClearPreset();
    }

    [Test]
    public void TestValidity()
    {
        for(var i = 0; i < 0b10000; i++)
        {
            var preset = new ApiPresetModel()
            {
                Name = "Test " + i.ToString(),

                ContentType = (i & 0b01000) != 0 ? "application/json" : string.Empty,
                TargetUrl = (i & 0b00100) != 0 ? "https://paci.dev/" : string.Empty,
                SentData = (i & 0b00010) != 0 ? "Test Data" : string.Empty,
                ResultField = (i & 0b00001) != 0 ? "result" : string.Empty,
            };

            var result = _apiClient.LoadPreset(preset);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsOk, i == 0b01111 ? Is.True : Is.False, "Expected result wrong");
                Assert.That(_apiClient.IsPresetValid(), i == 0b01111 ? Is.True : Is.False, "Expected preset validity wrong");
            });
        }
    }

    [Test]
    public void DoesLoadPreset()
    {
        Assert.That(_apiClient.IsPresetLoaded(), Is.False, "No preset should be loaded");
        var validPreset = GetValidApiPreset();
        _apiClient.LoadPreset(validPreset).AssertOk();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_apiClient.IsPresetLoaded(), Is.True, "Preset should be loaded");
            Assert.That(_apiClient.IsPresetValid(), Is.True, "Preset should be valid");
        }

        _apiClient.ClearPreset();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_apiClient.IsPresetLoaded(), Is.False, "Preset should not be loaded");
            Assert.That(_apiClient.IsPresetValid(), Is.False, "Preset should not be valid");
        }
    }

    [Test] 
    public async Task TestSendBytes()
    {
        var preset = GetUsageTestApiPreset();
        _apiClient.LoadPreset(preset).AssertOk();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_apiClient.IsPresetLoaded(), Is.True, "Preset should be loaded");
            Assert.That(_apiClient.IsPresetValid(), Is.True, "Preset should be valid");
        }

        _webClient.SendResult = USG_TEST_CONTENT_VALUE_FULL;
        byte[] dataToSend = Encoding.UTF8.GetBytes(USG_TEST_SEND_VALUE);
        var res = await _apiClient.SendBytesAsync(dataToSend);
        res.AssertOk();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(res.Value, Is.EqualTo(USG_TEST_CONTENT_VALUE), "Response did not get parsed correctly");
            Assert.That(_webClient.Requests, Has.Count.EqualTo(1), "Request was not sent");
        }

        var requestSent = _webClient.Requests[0];
        var content = requestSent.Content as ByteArrayContent;
        AssertMainContent(requestSent, preset, content);

        var textContent = Encoding.UTF8.GetString(await content!.ReadAsByteArrayAsync());
        Assert.That(textContent, Is.EqualTo(USG_TEST_SEND_VALUE), "Sent data is wrong");
    }

    [Test] 
    public async Task TestSendText()
    {
        var preset = GetUsageTestApiPreset();
        _apiClient.LoadPreset(preset).AssertOk();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_apiClient.IsPresetLoaded(), Is.True, "Preset should be loaded");
            Assert.That(_apiClient.IsPresetValid(), Is.True, "Preset should be valid");
        }

        _webClient.SendResult = USG_TEST_CONTENT_VALUE_FULL;
        var res = await _apiClient.SendTextAsync(USG_TEST_SEND_VALUE);
        res.AssertOk();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(res.Value, Is.EqualTo(USG_TEST_CONTENT_VALUE), "Response did not get parsed correctly");
            Assert.That(_webClient.Requests, Has.Count.EqualTo(1), "Request was not sent");
        }

        var requestSent = _webClient.Requests[0];
        var content = requestSent.Content as StringContent;
        AssertMainContent(requestSent, preset, content);

        var textContent = Encoding.UTF8.GetString(await content!.ReadAsByteArrayAsync());
        Assert.That(textContent, Is.EqualTo(preset.SentData.Replace("[T]", USG_TEST_SEND_VALUE)), "Sent data is wrong");
    }

    [Test]
    public async Task NotStartTest()
    {
        _webClient.SendResult = USG_TEST_CONTENT_VALUE_FULL;
        _webClient.Stop().AssertOk();

        //No preset
        (await _apiClient.SendTextAsync("Test")).AssertFail("Exception should be thrown as no preset is loaded");

        //Invalid preset
        var invalidPreset = new ApiPresetModel()
        {
            ContentType = string.Empty
        };
        _apiClient.LoadPreset(invalidPreset).AssertFail();
        var res = await _apiClient.SendTextAsync("Test");
        using (Assert.EnterMultipleScope())
        {
            res.AssertFail("Exception should be thrown as no preset invalid loaded");
            Assert.That(_apiClient.IsPresetLoaded(), Is.False);
        }

        //No client
        var validPreset = GetValidApiPreset();
        _apiClient.LoadPreset(validPreset).AssertOk();
        using (Assert.EnterMultipleScope())
        {
            res.AssertFail("Exception should be thrown as no web client is started");
            Assert.That(_apiClient.IsPresetLoaded(), Is.True);
        }

        //OK
        _webClient.Start().AssertOk();
        (await _apiClient.SendTextAsync("Test")).AssertOk("Exception should not be thrown as all is loaded");
    }

    [Test]
    public async Task TimeoutHandling()
    {
        var preset = GetValidApiPreset();
        preset.ConnectionTimeout = 25;
        _webClient.SendResult = USG_TEST_CONTENT_VALUE_FULL;
        _apiClient.LoadPreset(preset).AssertOk();

        _webClient.ArtificialDelayMs = 30;
        using (Assert.EnterMultipleScope())
        {
            (await _apiClient.SendTextAsync("Test")).AssertFail("Send should time out");
            (await _apiClient.SendBytesAsync([0])).AssertFail("Send should time out");
        }
        Assert.That(_webClient.Requests, Has.Count.EqualTo(2), "2 requests should have been sent");

        _webClient.Requests.Clear();
        _webClient.ArtificialDelayMs = 20;
        using (Assert.EnterMultipleScope())
        {
            (await _apiClient.SendTextAsync("Test")).AssertOk("Send should not time out");
            (await _apiClient.SendBytesAsync([0])).AssertOk("Send should not time out");
        }
        Assert.That(_webClient.Requests, Has.Count.EqualTo(2), "2 requests should have been sent");
    }

    protected override void OneTimeTearDownExtra()
    {
        if (_apiClient.IsPresetLoaded())
        {
            _apiClient.ClearPreset();
        }
        _webClient.Stop().AssertOk();
    }
    
    private const string TEST_CONTENT_TYPE = "application/json";
    private const string TEST_TARGET_URL = "https://paci.dev/";
    private const string TEST_RESULT_FIELD = "result";
    private const string TEST_SENT_DATA = @"{""data"" : ""[T]""}";
    private ApiPresetModel GetValidApiPreset()
    {
        return new ApiPresetModel()
        {
            Name = "Valid API",

            ContentType = TEST_CONTENT_TYPE,
            TargetUrl = TEST_TARGET_URL,
            ResultField = TEST_RESULT_FIELD,
            SentData = TEST_SENT_DATA
        };
    }

    private const string USG_TEST_HEADER_KEY = "Header";
    private const string USG_TEST_HEADER_VALUE = "Value";
    private const string USG_TEST_AUTH_SCHEME = "Key-Test";
    private const string USG_TEST_AUTH_PARAMETER = "A";
    private const string USG_TEST_AUTH_FULL = $"{USG_TEST_AUTH_SCHEME} {USG_TEST_AUTH_PARAMETER}";
    private const string USG_TEST_HOST_KEY = "Host";
    private const string USG_TEST_HOST_VALUE = "HostingTest";
    private const string USG_TEST_CONTENT_VALUE = "Test 2";
    private const string USG_TEST_CONTENT_VALUE_FULL = $@"{{""data"": ""Test1"", ""{TEST_RESULT_FIELD}"": ""{USG_TEST_CONTENT_VALUE}""}}";
    private const string USG_TEST_SEND_VALUE = "Hiii Test!";
    private ApiPresetModel GetUsageTestApiPreset()
    {
        var preset = GetValidApiPreset();
        preset.Authorization = USG_TEST_AUTH_FULL;
        preset.HeaderValues.Add(USG_TEST_HEADER_KEY, USG_TEST_HEADER_VALUE);
        preset.HeaderValues.Add(USG_TEST_HOST_KEY, USG_TEST_HOST_VALUE);
        return preset;
    }

    private void AssertMainContent(HttpRequestMessage requestSent, ApiPresetModel preset, HttpContent? content)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(requestSent.Content, Is.Not.Null, "Content sent would be null");
            Assert.That(requestSent.RequestUri?.AbsoluteUri, Is.EqualTo(preset.TargetUrl), "Target was set wrong");
            Assert.That(requestSent.Headers.Host, Is.EqualTo(USG_TEST_HOST_VALUE), "Host header wrong");
            Assert.That(requestSent.Method, Is.EqualTo(HttpMethod.Post), "Wrong method used");
            Assert.That(requestSent.Headers.NonValidated[USG_TEST_HEADER_KEY].FirstOrDefault(), Is.EqualTo(USG_TEST_HEADER_VALUE), "Header test failed");
            Assert.That(requestSent.Headers.Authorization?.Scheme, Is.EqualTo(USG_TEST_AUTH_SCHEME), "Auth Scheme wrong");
            Assert.That(requestSent.Headers.Authorization?.Parameter, Is.EqualTo(USG_TEST_AUTH_PARAMETER), "Auth Parameter wrong");
        }
        Assert.That(content!.Headers.ContentType?.MediaType, Is.EqualTo(preset.ContentType), "Wrong content type");
    }
}