using System;
using Serilog;

namespace Hoscy.Services.DependencyCore;

public class PrototypeLoadIntoDiContainer(Type asType, Lifetime lifetime = Lifetime.Singleton) : LoadIntoDiContainerAttribute(asType, lifetime)
{
    public void NotifyAboutLoadedPrototype(Type impl, ILogger logger)
    {
        logger.Fatal($"!!! LOADED PROTOTYPE {impl.FullName} AS {AsType.FullName} - THIS SHOULD ONLY BE USED FOR TESTING !!!");
    }
}