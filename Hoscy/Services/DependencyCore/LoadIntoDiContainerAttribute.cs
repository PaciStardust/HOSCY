using System;

namespace Hoscy.Services.DependencyCore;

/// <summary>
/// Flag for enabling automatic loading of services, lifetime attributes do not affect hosted services
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class LoadIntoDiContainerAttribute(Lifetime lifetime = Lifetime.Singleton) : Attribute
{
    public Lifetime Lifetime { get; } = lifetime;
}

public enum Lifetime
{
    Singleton,
    Scoped,
    Transient
}