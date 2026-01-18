using Serilog;

namespace HoscyCore.Services.DependencyCore;

public class PrototypeLoadIntoDiContainer(Type asType, Lifetime lifetime = Lifetime.Singleton) : LoadIntoDiContainerAttribute(asType, lifetime)
{
    public void NotifyAboutLoadedPrototype(Type impl, ILogger logger)
    {
        logger.Fatal("!!! LOADED PROTOTYPE \"{implName}\" AS \"{asTypeName}\" - THIS SHOULD ONLY BE USED FOR TESTING !!!", impl.FullName, AsType.FullName);
    }
}