using Serilog;

namespace HoscyCore.Services.Interfacing;

public class KeepaliveTimer : IDisposable
{
    private readonly System.Timers.Timer _timer;
    public event Action OnKeepaliveFailed = delegate { };
    private DateTimeOffset _lastKeepaliveReceived = DateTimeOffset.MinValue;
    private readonly TimeSpan _timeLimit;
    private readonly ILogger _logger;

    public KeepaliveTimer(ILogger logger, TimeSpan timeLimit)
    {
        _logger = logger.ForContext<KeepaliveTimer>();
        _timer = new()
        {
            AutoReset = true,
            Interval = 1000,
        };
        _timer.Elapsed += (_, _) => TimerTick();
        _timeLimit = timeLimit;
    }

    private void TriggerKeepalive()
        => _lastKeepaliveReceived = DateTimeOffset.UtcNow;

    private void TimerTick()
    {
        if (DateTimeOffset.UtcNow > _lastKeepaliveReceived + _timeLimit)
        {
            Stop();
            _logger.Debug("Keepalive failed");
            OnKeepaliveFailed.Invoke();
        }
    }

    public void Start()
    {
        if (_timer.Enabled) return;
        _logger.Debug("Keepalive timer started");
        TriggerKeepalive();
        _timer.Enabled = true;
    }

    public void Stop()
    {
        if (!_timer.Enabled) return;
        _logger.Debug("Keepalive timer stopped");
        _timer.Enabled = false;
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
    }
}