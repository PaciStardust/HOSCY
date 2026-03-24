using Serilog;

namespace HoscyCore.Ipc;

public class KeepAliveTimer : IDisposable
{
    private readonly ILogger _logger;

    private readonly System.Timers.Timer _aliveCheckTimer;
    public event Action OnKeepAliveFailed = delegate { };
    private DateTimeOffset _lastKeepAliveReceived = DateTimeOffset.MinValue;
    private readonly TimeSpan _timeLimit;

    private readonly System.Timers.Timer _aliveSendTimer;
    public event Action<uint> OnKeepAliveSend = delegate { };

    public KeepAliveTimer(ILogger logger, TimeSpan timeLimit, double checkInterval = 1000, double sendInterval = 2500)
    {
        _logger = logger;
        _aliveCheckTimer = new()
        {
            AutoReset = true,
            Interval = checkInterval,
        };
        _aliveCheckTimer.Elapsed += (_, _) => TimerTickCheck();
        _timeLimit = timeLimit;

        _aliveSendTimer = new()
        {
            AutoReset = true,
            Interval = sendInterval
        };
        _aliveCheckTimer.Elapsed += (_, _) => TimerTickSend();
    }

    public void TriggerKeepAlive()
        => _lastKeepAliveReceived = DateTimeOffset.UtcNow;

    private void TimerTickCheck()
    {
        if (DateTimeOffset.UtcNow > _lastKeepAliveReceived + _timeLimit)
        {
            Stop();
            _logger.Debug("KeepAlive: Failed");
            OnKeepAliveFailed.Invoke();
        }
    }

    private uint _sendIndex = 0;
    private void TimerTickSend()
    {
        OnKeepAliveSend.Invoke(_sendIndex++);
    }

    public void Start()
    {
        if (_aliveCheckTimer.Enabled) return;
        _logger.Debug("KeepAlive: Timer started");
        TriggerKeepAlive();
        _aliveSendTimer.Enabled = true;
        _aliveCheckTimer.Enabled = true;
    }

    public void Stop()
    {
        if (!_aliveCheckTimer.Enabled) return;
        _logger.Debug("KeepAlive: Timer stopped");
        _aliveCheckTimer.Enabled = false;
        _aliveSendTimer.Enabled = false;
    }

    public void Dispose()
    {
        Stop();
        _aliveCheckTimer.Dispose();
        _aliveSendTimer.Dispose();
    }
}