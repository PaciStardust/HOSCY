namespace HoscyCore.Services.DependencyCore;

/// <summary>
/// Flag for enabling automatic loading of services, lifetime attributes do not affect hosted services
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class LoadIntoDiContainerAttribute(Type asType, Lifetime lifetime = Lifetime.Singleton) : Attribute
{
    public Lifetime Lifetime { get; } = lifetime;
    public Type AsType { get; } = asType;
}

public enum Lifetime
{
    Singleton,
    Scoped,
    Transient
}