namespace HoscyCore.Services.Dependency;

/// <summary>
/// Flag for enabling automatic loading of services, lifetime attributes do not affect hosted services
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class LoadIntoDiContainerAttribute(Type asType, Lifetime lifetime = Lifetime.Singleton, SupportedPlatformFlags platforms = SupportedPlatformFlags.All) : Attribute
{
    public Lifetime Lifetime { get; } = lifetime;
    public Type AsType { get; } = asType;
    public SupportedPlatformFlags SupportedPlatforms { get; } = platforms;
}

public enum Lifetime
{
    Singleton,
    Scoped,
    Transient
}

[Flags]
public enum SupportedPlatformFlags
{
    Windows = 0b1,
    Linux = 0b10,
    OSX = 0b100,
    All = Windows | Linux | OSX
}