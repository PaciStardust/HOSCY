
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Events;

namespace HoscyCore.Utility;

public static class ResC
{
    public static Res Ok()
        => Res.Ok();

    public static Res<T> TOk<T>(T value) where T : notnull
        => Res<T>.Ok(value);

    public static Res Fail(ResMsg message)
        => Res.Fail(message);
    public static Res Fail(string context, ResMsgLvl lvl = ResMsgLvl.Error)
        => Res.Fail(new(lvl, $"Unknown fail from {context}"));
    public static Res FailM(params IEnumerable<ResMsg?> messages)
        => Fail(ResMsg.Combine(messages));

    public static Res<T> TFail<T>(ResMsg message) where T : notnull
        => Res<T>.Fail(message);
    public static Res<T> TFail<T>(string context, ResMsgLvl lvl = ResMsgLvl.Error) where T : notnull
        => TFail<T>(new(lvl, $"Unknown fail from {context}"));
    public static Res<T> TFailM<T>(params IEnumerable<ResMsg?> messages) where T : notnull
        => TFail<T>(ResMsg.Combine(messages));

    public static Res FailLog(string message, ILogger? logger, Exception? ex = null, ResMsgLvl lvl = ResMsgLvl.Error)
    {
        var msg = Log(message, logger, ex, lvl);
        return Fail(new ResMsg(lvl, msg));
    }
    public static Res<T> TFailLog<T>(string message, ILogger? logger, Exception? ex = null, ResMsgLvl lvl = ResMsgLvl.Error) where T : notnull
    {
        var msg = Log(message, logger, ex, lvl);
        return TFail<T>(new ResMsg(lvl, msg));
    }
    public static string Log(string message, ILogger? logger, Exception? ex = null, ResMsgLvl lvl = ResMsgLvl.Error)
    {
        if (logger is not null)
        {
            LogEventLevel logLevel = lvl switch
            {
                ResMsgLvl.Info => LogEventLevel.Information,
                ResMsgLvl.Warning => LogEventLevel.Warning,
                ResMsgLvl.Error => LogEventLevel.Error,
                ResMsgLvl.Fatal => LogEventLevel.Fatal,
                _ => LogEventLevel.Error
            };
            logger.Write(logLevel, ex, message);
        }
        
        return ex is null ? message : ResMsg.FmtEx(ex, message);
    }

    public static Res Wrap(Func<Res> unsafeFunc, string message, ILogger? logger, ResMsgLvl lvl = ResMsgLvl.Error)
    {
        try
        {
            return unsafeFunc();
        }
        catch (Exception ex)
        {
            return FailLog(message, logger, ex, lvl);
        }
    }
    public static Res WrapR(Action unsafeAction, string message, ILogger? logger, ResMsgLvl lvl = ResMsgLvl.Error)
    {
        return Wrap(() =>
        {
            unsafeAction();
            return Ok();
        }, message, logger, lvl);
    }

    public static Res<T> TWrap<T>(Func<Res<T>> unsafeFunc, string message, ILogger? logger, ResMsgLvl lvl = ResMsgLvl.Error) where T : notnull
    {
        try
        {
            return unsafeFunc();
        }
        catch (Exception ex)
        {
            return TFailLog<T>(message, logger, ex, lvl);
        }
    }
    public static Res<T> TWrapR<T>(Func<T> unsafeFunc, string message, ILogger? logger, ResMsgLvl lvl = ResMsgLvl.Error) where T : notnull
    {
        return TWrap(() =>
        {
            return TOk(unsafeFunc());
        }, message, logger, lvl);
    }
}

public abstract record ResBase
{
    [MemberNotNullWhen(false, nameof(Msg))]
    public virtual bool IsOk { get; init; }
    public ResMsg? Msg { get; init; }    

    public void IfFail(Action<ResMsg> action)
    {
        if (!IsOk)
        {
            action.Invoke(Msg);
        }
    }
}

public record Res : ResBase
{
    protected Res() { }

    private static readonly Res _ok = new() { IsOk = true, Msg = null };
    public static Res Ok()
        => _ok;
    public static Res Fail(ResMsg message)
        => new() { IsOk = false, Msg = message };

    public override string ToString()
        => Msg is null
            ? $"Result = {(IsOk ? "Ok" : "Fail")}"
            : Msg.ToString();
}

public record Res<T> : ResBase where T: notnull 
{
    [MemberNotNullWhen(true, nameof(Value))] 
    public override bool IsOk { get; init; }
    public T? Value { get; init; }

    protected Res() { }

    public static Res<T> Ok(T value)
        => value is null
            ? Fail(ResMsg.Err("Result was attempted to be set to a null value"))
            : new() { IsOk = true, Value = value };
    public static Res<T> Fail(ResMsg message)
        => new() { IsOk = false, Msg = message };

    public override string ToString()
        => Msg is null
            ? $"Result<{typeof(T).Name}>={(IsOk ? "Ok" : "Fail")}"
            : $"Result<{typeof(T).Name}>={(IsOk ? "Ok" : "Fail")} => {Msg}";
}

public record ResMsg(ResMsgLvl Level, string Message)
{
    public static ResMsg Inf(string message)
        => new(ResMsgLvl.Info, message);
    public static ResMsg Wrn(string message)
        => new(ResMsgLvl.Warning, message);
    public static ResMsg Err(string message)
        => new(ResMsgLvl.Error, message);
    public static ResMsg Ftl(string message)
        => new(ResMsgLvl.Fatal, message);

    public ResMsg WithContext(string context)
        => new(Level, $"[{context}] {Message}");

    public static ResMsg Combine(params IEnumerable<ResMsg?> messages)
    {
        var messageArray = messages.Where(x => x is not null).ToArray();
        if (messageArray.Length == 0)
            return Inf("Empty combined result message");

        if (messageArray.Length == 1)
            return messageArray[0]!;

        var level = messageArray.Max(x => x!.Level);
        var messagesParsed = messageArray.Select(x => $" - {x!.Level} => {x.Message}");

        return new ResMsg(level, $"Multiple messages were returned:\n{string.Join("\n", messagesParsed)}");
    }

    public static string FmtEx(Exception ex, string message)
        => $"{message}: {ex.Message}";

    public override string ToString()
        => $"{Level} => {Message}";
};

public enum ResMsgLvl
{
    Info,
    Warning,
    Error,
    Fatal
}