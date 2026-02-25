using Serilog;

namespace HoscyCore.Services.Dependency;

public class PrototypeLoadIntoDiContainer(Type asType, Lifetime lifetime = Lifetime.Singleton, SupportedPlatformFlags platforms = SupportedPlatformFlags.All) : LoadIntoDiContainerAttribute(asType, lifetime, platforms)
{
    public void NotifyAboutLoadedPrototype(Type impl, ILogger logger)
    {
        logger.Fatal("!!! LOADED PROTOTYPE \"{implName}\" AS \"{asTypeName}\" - THIS SHOULD ONLY BE USED FOR TESTING !!!", impl.FullName, AsType.FullName);
    }
}