using System.Threading.Channels;
using Serilog;

namespace HoscyCore.Utility;

public abstract class AsyncProcessingLoop<T>(ILogger logger) : IDisposable //todo: [TEST] Write tests for this
{
    private readonly ILogger _logger = logger;
    private readonly Channel<(DateTimeOffset CreatedAt, T Item)> _channel = Channel.CreateUnbounded<(DateTimeOffset, T)>(new()
    {
        SingleReader = true,
        SingleWriter = true
    });
    private readonly CancellationTokenSource _cts = new();
    private Task? _currentTask;
    
    protected abstract Task HandleItem(T item);
    protected abstract void HandleClearedItem(T item);

    public bool IsRunning
        => _currentTask is not null && !_currentTask.IsCompleted;

    protected bool Enqueue(T item)
    {
        if (_channel.Writer.TryWrite((DateTimeOffset.UtcNow, item)))
        {
            _logger.Verbose("Added item {item} to queue", item);
            return true;
        }

        _logger.Verbose("Failed to add item {item} to queue, it is likely closed", item);
        return false;
    }

    private DateTimeOffset _ignoreItemsBeforeThisTime = DateTimeOffset.MinValue;
    protected async Task Run(CancellationToken ct)
    {

        ulong itemCount = 0;
        while (await _channel.Reader.WaitToReadAsync(ct))
        {
            if (!_channel.Reader.TryRead(out var item)) continue;
            itemCount++;

            if (item.CreatedAt <= _ignoreItemsBeforeThisTime)
            {
                _logger.Verbose("Clearing item {count} ({item})", itemCount, item.Item);
                HandleClearedItem(item.Item);
                return;
            }

            _logger.Verbose("Processing item {count} ({item})", itemCount, item.Item);
            await HandleItem(item.Item);
        }
    }

    public void Start()
    {
        if (_cts.IsCancellationRequested || _currentTask is not null) return;
        _logger.Debug("Starting loop");
        _currentTask = Run(_cts.Token);
    }

    public Res Stop()
    {
        if (_currentTask is null) return ResC.Ok();

        _logger.Debug("Stopping loop");
        _cts.Cancel();

        var res =  LaunchUtils.SafelyWaitForTaskWithTimeoutAndReturnException(_currentTask, 250,
            new("Failed to stop handler task"), _logger);

        if (res.IsOk)
            _logger.Debug("Stopped loop");
        else
            _logger.Debug("Failed stopping loop ({result})", res);

        return res;
    }

    public void Clear()
    {
        _ignoreItemsBeforeThisTime = DateTimeOffset.UtcNow;
    }

    public void Dispose()
    {
        _currentTask?.Dispose();
        _cts.Dispose();
    }
}