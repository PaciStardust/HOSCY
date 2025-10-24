using System;
using System.Diagnostics.CodeAnalysis;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Translation.Core;

public interface ITranslator : IStartStopService
{
    public event EventHandler<Exception> OnRuntimeError;
    public event EventHandler OnShutdownCompleted;
    public string GetName();
    public bool TryTranslate(string input, [NotNullWhen(true)] out string? output);
}