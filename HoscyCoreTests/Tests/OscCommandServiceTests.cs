using System.Text;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Osc.Command;
using HoscyCore.Services.Osc.Query;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests;

public class OscCommandServiceTests : TestBaseForService<OscCommandServiceTests>
{
    private OscCommandService _commandService = null!;
    private OscQueryHostRegistry _registry = null!;
    private MockOscSendService _sender = null!;
    private readonly ConfigModel _config = new();

    protected override void OneTimeSetupExtra()
    {
        _registry = new(_logger);
        _sender = new(_config);
        _commandService = new(_logger, _registry, _sender);

        _commandService.Start();
        AssertServiceProcessing(_commandService);
    }

    protected override void SetupExtra()
    {
        _sender.ReceivedMessages.Clear();
    }

    [Test]
    public void IsPrefixDetected()
    {
        var result = _commandService.DetectCommand("Test");
        Assert.That(result, Is.False);

        result = _commandService.DetectCommand("[Test]");
        Assert.That(result, Is.False);

        result = _commandService.DetectCommand(_commandService.CommandIdentifier);
        Assert.That(result, Is.True);
    }

    [Test]
    public void AddressTest()
    {
        var targets = new List<(string Message, OscCommandState state)> {
            ("Waaaa",           OscCommandState.Malformed),
            ("/",               OscCommandState.Malformed),
            ("/abc",            OscCommandState.Success),
            ("/123",            OscCommandState.Success),
            ("/***",            OscCommandState.Malformed),
            ("/abc/def",        OscCommandState.Success),
            ("/abc/123/456",    OscCommandState.Success),
            ("//",              OscCommandState.Malformed),
            ("/abc/",           OscCommandState.Malformed),
            ("/abcdef//abc",    OscCommandState.Malformed)
        };

        var arrayIndex = 0;
        foreach (var (addr, handle) in targets)
        {
            var fullMessage = $"[OSC] [{addr} [b]true]";
            var result = _commandService.DetectAndHandleCommand(fullMessage);
            Assert.That(result, Is.EqualTo(handle), $"Failed on {addr}");

            if (handle != OscCommandState.Success) continue;
            Thread.Sleep(50);
            Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(arrayIndex + 1),$"Failed on {addr}");
            Assert.That(_sender.ReceivedMessages[arrayIndex].Address, Is.EqualTo(addr), $"Failed on {addr}");
            arrayIndex++;
        }
    }

    [Test]
    public void VariableTest()
    {
        var possibleVariables = new List<(string Parameter, bool Valid, object? ExpectedValue)>
        {
            ("[s]123",          false,  null),
            ("[S]\"test\"",     true,   "test"),
            ("[s]\"test2\"",    true,   "test2"),
            ("[s]\"tes 2\"",    true,   "tes 2"),
            ("[S]\"aaaaa",      false,  null),
            ("[b]true",         true,   true),
            ("[b]false",        true,   false),
            ("[B]f",            true,   false),
            ("[b]1",            true,   true),
            ("[b]0",            true,   false),
            ("[B]2",            false,  null),
            ("[i]123",          true,   123),
            ("[I]-1245",        true,   -1245),
            ("[i]tests",        false,  null),
            ("[i]23c4c",        false,  null),
            ("[i]0123",         true,   123),
            ("[i]0",            true,   0),
            ("[f]1.2",          true,   1.2f),
            ("[F]-123.456",     true,   -123.456f),
            ("[f]1,234",        false,  null),
            ("[F]100.0",        true,   100f),
            ("[F]123ba",        false,  null),
            ("[f]1001",         true,   1001f),
            ("[f]-1001111.24",  true,   -1001111.24f),
        };

        var r = new Random();

        var resultIndex = 0;
        for(var i = 0; i < 256; i++)
        {
            var paramCount = r.Next(3) + 1;
            var paramList = new List<(string Parameter, bool Valid, object? ExpectedValue)>();
            for (var ii = 0; ii < paramCount; ii++)
            {
                paramList.Add(possibleVariables[r.Next(possibleVariables.Count)]);
            }
            
            var expectedResult = paramList.All(x => x.Valid) ? OscCommandState.Success : OscCommandState.Malformed;
            var paramString = string.Join(" ", paramList.Select(x => x.Parameter));
            var fullMessage = $"[OSC] [/test {paramString}]";
            var result = _commandService.DetectAndHandleCommand(fullMessage);
            Assert.That(result, Is.EqualTo(expectedResult),$"Failed on {paramString}");

            if (result != OscCommandState.Success) continue;
            Thread.Sleep(50);
            Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(resultIndex + 1),$"Failed on {paramString}");
            Assert.That(_sender.ReceivedMessages[resultIndex].Args, Is.EqualTo(paramList.Select(x => x.ExpectedValue).ToArray()), $"Failed on {paramString}");

            resultIndex++;
        }
    }

    [Test]
    public void TargetingTest()
    {
        var possibleVariables = new List<(string Text, bool Valid, string? Ip, ushort? Port)>
        {
            (string.Empty,      true,     _config.Osc_Routing_TargetIp, _config.Osc_Routing_TargetPort),
            (":",               true,     _config.Osc_Routing_TargetIp, _config.Osc_Routing_TargetPort),
            ("127.0.0.1:",      true,     "127.0.0.1", _config.Osc_Routing_TargetPort),
            ("0.0.0.0:",        true,     "0.0.0.0", _config.Osc_Routing_TargetPort),
            ("255.255.255.255:",true,     "255.255.255.255", _config.Osc_Routing_TargetPort),
            (":9021",           true,     _config.Osc_Routing_TargetIp, 9021),
            (":1",              true,     _config.Osc_Routing_TargetIp, 1),
            (":65535",          true,     _config.Osc_Routing_TargetIp, 65535),
            ("127.0.0.1:9021",  true,     "127.0.0.1", 9021),
            ("[target]",        false,    null, null),
            ("[target]:8000",   false,    null, null),
            ("test:8000",       false,    null, null),
            ("127.0.0.1:65536", false,    null, null),
            ("127.0.0.1:-1",    false,    null, null),
            ("127.0.0.:100" ,   false,    null, null),
            ("127:100" ,        false,    null, null),
            ("127.0.0.0.0:100", false,    null, null),
            ("[target]:-1",     false,    null, null),
        };

        var arrayIndex = 0;
        foreach (var (text, valid, ip, port) in possibleVariables)
        {
            var fullMessage = $"[OSC] [/test [b]true {text}]";
            var result = _commandService.DetectAndHandleCommand(fullMessage);
            Assert.That(result, Is.EqualTo(valid ? OscCommandState.Success : OscCommandState.Malformed), $"Failed on {text}");

            if (!valid) continue;
            Thread.Sleep(50);
            Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(arrayIndex + 1),$"Failed on {text}");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sender.ReceivedMessages[arrayIndex].Ip, Is.EqualTo(ip), $"Failed on {text}");
                Assert.That(_sender.ReceivedMessages[arrayIndex].Port, Is.EqualTo(port), $"Failed on {text}");
            }
            arrayIndex++;
        }
    }

    [Test]
    public void TargetingQueryTest()
    {   
        const string IP = "1.2.4.8";
        const ushort PORT = 1248;

        _registry.SetSelf(IP, PORT);
        var possibleVariables = new List<(string Text, bool Valid, string? Ip, ushort? Port)>
        {
            (string.Empty,      true,     _config.Osc_Routing_TargetIp, _config.Osc_Routing_TargetPort),
            ("\"self\"",        true,     IP, PORT),
            ("self",            false,    null, null),
            ("other",           false,    null, null),
            (":9011 \"self\"",  true,     IP, 9011),
            ("1.1.1.1:9011 \"self\"", true, "1.1.1.1", 9011),
            ("1.1.1.1: \"self\"", true, "1.1.1.1", PORT),
            ("\"self\":9011",   false,    null, null),
        };

        var arrayIndex = 0;
        foreach (var (text, valid, ip, port) in possibleVariables)
        {
            var fullMessage = $"[OSC] [/test [b]true {text}]";
            var result = _commandService.DetectAndHandleCommand(fullMessage);
            Assert.That(result, Is.EqualTo(valid ? OscCommandState.Success : OscCommandState.Malformed), $"Failed on {text}");

            if (!valid) continue;
            Thread.Sleep(50);
            Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(arrayIndex + 1),$"Failed on {text}");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sender.ReceivedMessages[arrayIndex].Ip, Is.EqualTo(ip), $"Failed on {text}");
                Assert.That(_sender.ReceivedMessages[arrayIndex].Port, Is.EqualTo(port), $"Failed on {text}");
            }
            arrayIndex++;
        }
    }

    [Test]
    public void MultiTest()
    {
        const string MSG_BASE = "[/t% [b]true]";

        var r = new Random();
        for (var i = 0; i < 64; i++)
        {
            var count = r.Next(2, 9);
            var sb = new StringBuilder("[OSC]");
            var fails = 0;
            for (var ii = 0; ii < count; ii++)
            {
                if (r.Next(4) == 0)
                {
                    sb.Append($" {MSG_BASE.Replace("%", "//")}"); //fails
                    fails++;
                } else
                {
                    sb.Append($" {MSG_BASE.Replace("%", ii.ToString())}");
                }
            }

            _sender.ReceivedMessages.Clear();
            var text = sb.ToString();
            var result = _commandService.DetectAndHandleCommand(text);
            Assert.That(result, Is.EqualTo(fails == count ? OscCommandState.Malformed : OscCommandState.Success), $"Failed on {text}");

            Thread.Sleep(50);
            Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(count - fails));
        }
    }

    [Test]
    public void WaitTest()
    {
        var possibleVariables = new List<(string Text, bool Valid, int? ExpectedWait)>
        {
            ("w200",    true,   200),
            ("w400",    true,   400),
            ("200",     false,  null),
            ("50w",     false,  null),
            ("w0a",     false,  null),
            ("w-100",   false,  null)
        };

        foreach (var (text, valid, wait) in possibleVariables)
        {
            _sender.ReceivedMessages.Clear();

            var fullMessage = $"[OSC] [/test [b]true {text}] [/test2 [b]false]";
            var result = _commandService.DetectAndHandleCommand(fullMessage);
            Assert.That(result, Is.EqualTo(OscCommandState.Success), $"Failed on {text}");

            if (valid)
            {
                Thread.Sleep(wait!.Value - 50);
            
                Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(1));
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(_sender.ReceivedMessages[0].Address, Is.EqualTo("/test"));
                    Assert.That(_sender.ReceivedMessages[0].Args, Has.Length.EqualTo(1));
                }
                Assert.That(_sender.ReceivedMessages[0].Args[0], Is.True);

                Thread.Sleep(100);
                
                Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(2));
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(_sender.ReceivedMessages[1].Address, Is.EqualTo("/test2"));
                    Assert.That(_sender.ReceivedMessages[1].Args, Has.Length.EqualTo(1));
                }
                Assert.That(_sender.ReceivedMessages[1].Args[0], Is.False);
            }
            else
            {
                Thread.Sleep(50);
                Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(1));
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(_sender.ReceivedMessages[0].Address, Is.EqualTo("/test2"));
                    Assert.That(_sender.ReceivedMessages[0].Args, Has.Length.EqualTo(1));
                }
                Assert.That(_sender.ReceivedMessages[0].Args[0], Is.False);
            }
        }
    }

    [Test]
    public void AllAtOnceTest()
    {
        const ushort REG_PORT = 42420;
        const string REG_IP = "1.0.255.10";
        _registry.SetSelf(REG_IP, REG_PORT);

        var message = "[OSC] [/test/step/1 [s]\"Hello World\" [b]t [B]False [i]-4767 [f]0.001 10.0.0.4: \"self\" w250] [/test/step/2 [f]-0.001 [i]4767 [B]True [b]f [s]\"Goodbye World\" :9876 \"self\"]";
        var result = _commandService.DetectAndHandleCommand(message);

        Assert.That(result, Is.EqualTo(OscCommandState.Success), "Command was not valid");
        Thread.Sleep(200);

        Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sender.ReceivedMessages[0].Address, Is.EqualTo("/test/step/1"));
            Assert.That(_sender.ReceivedMessages[0].Ip, Is.EqualTo("10.0.0.4"));
            Assert.That(_sender.ReceivedMessages[0].Port, Is.EqualTo(REG_PORT));
            Assert.That(_sender.ReceivedMessages[0].Args, Has.Length.EqualTo(5));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sender.ReceivedMessages[0].Args[0], Is.EqualTo("Hello World"));
            Assert.That(_sender.ReceivedMessages[0].Args[1], Is.True);
            Assert.That(_sender.ReceivedMessages[0].Args[2], Is.False);
            Assert.That(_sender.ReceivedMessages[0].Args[3], Is.EqualTo(-4767));
            Assert.That(_sender.ReceivedMessages[0].Args[4], Is.EqualTo(0.001f));
        }

        Thread.Sleep(100);

        Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sender.ReceivedMessages[1].Address, Is.EqualTo("/test/step/2"));
            Assert.That(_sender.ReceivedMessages[1].Ip, Is.EqualTo(REG_IP));
            Assert.That(_sender.ReceivedMessages[1].Port, Is.EqualTo(9876));
            Assert.That(_sender.ReceivedMessages[1].Args, Has.Length.EqualTo(5));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sender.ReceivedMessages[1].Args[0], Is.EqualTo(-0.001f));
            Assert.That(_sender.ReceivedMessages[1].Args[1], Is.EqualTo(4767));
            Assert.That(_sender.ReceivedMessages[1].Args[2], Is.True);
            Assert.That(_sender.ReceivedMessages[1].Args[3], Is.False);
            Assert.That(_sender.ReceivedMessages[1].Args[4], Is.EqualTo("Goodbye World"));
        }
    }

    protected override void OneTimeTearDownExtra()
    {
        _commandService.Stop();
        AssertServiceStopped(_commandService);
    }
}