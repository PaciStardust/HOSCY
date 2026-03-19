using Serilog;

namespace HoscyCore.Ipc;

public class KeepAliveTimer : IDisposable
{
    private readonly System.Timers.Timer _timer;
    public event Action OnKeepAliveFailed = delegate { };
    private DateTimeOffset _lastKeepAliveReceived = DateTimeOffset.MinValue;
    private readonly TimeSpan _timeLimit;
    private readonly ILogger _logger;

    public KeepAliveTimer(ILogger logger, TimeSpan timeLimit)
    {
        _logger = logger;
        _timer = new()
        {
            AutoReset = true,
            Interval = 1000,
        };
        _timer.Elapsed += (_, _) => TimerTick();
        _timeLimit = timeLimit;
    }

    private void TriggerKeepAlive()
        => _lastKeepAliveReceived = DateTimeOffset.UtcNow;

    private void TimerTick()
    {
        if (DateTimeOffset.UtcNow > _lastKeepAliveReceived + _timeLimit)
        {
            Stop();
            _logger.Debug("KeepAlive: Failed");
            OnKeepAliveFailed.Invoke();
        }
    }

    public void Start()
    {
        if (_timer.Enabled) return;
        _logger.Debug("KeepAlive: Timer started");
        TriggerKeepAlive();
        _timer.Enabled = true;
    }

    public void Stop()
    {
        if (!_timer.Enabled) return;
        _logger.Debug("KeepAlive: Timer stopped");
        _timer.Enabled = false;
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
    }
}