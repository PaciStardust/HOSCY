using HoscyCore.Services.Core;
using HoscyCoreTests.Mocks.Base;
using HoscyCoreTests.Mocks.Impl;

namespace HoscyCoreTests.Utils;

public abstract class StartStopModuleControllerTestBase<TLog, TModuleStartInfoBase, TModuleStartInfoImpl, TModuleBase, TModuleSubA, TModuleSubB, TController> 
    : TestBase<TLog>
    where TModuleStartInfoBase : class, ISoloModuleStartInfo
    where TModuleStartInfoImpl : MockSoloModuleStartInfoBase, TModuleStartInfoBase, new()
    where TModuleBase : class, IStartStopModule
    where TModuleSubA : class, TModuleBase, new()
    where TModuleSubB : class, TModuleBase, new()
    where TController : class, IStartStopModuleController<TModuleStartInfoBase, TModuleBase>
{
    protected MockBackToFrontNotifyService _notify = null!;
    protected MockContainerBulkLoader<TModuleStartInfoBase> _infoLoader = null!;
    protected MockContainerBulkLoader<TModuleBase> _moduleLoader = null!;

    protected TModuleSubA _moduleA = null!;
    protected TModuleSubB _moduleB = null!;

    protected TModuleStartInfoImpl _infoA = null!;
    protected TModuleStartInfoImpl _infoB = null!;
    protected TModuleStartInfoImpl _infoC = null!;

    protected TController _controller = null!;
    protected abstract TController CreateController();

    protected void SetupSharedClasses()
    {
        _notify = new();

        _moduleA = new();
        _moduleB = new();

        _infoA = new()
        {
            Name = "MockA",
            Description = "MockA",
            ModuleType = typeof(TModuleSubA)
        };
        _infoB = new()
        {
            Name = "MockB",
            Description = "MockB",
            ModuleType = typeof(TModuleSubB)
        };
        _infoC = new()
        {
            Name = "MockC",
            Description = "MockC",
            ModuleType = typeof(TModuleStartInfoImpl)
        };

        _infoLoader = new(() => [ _infoA, _infoB, _infoC ]);
        _moduleLoader = new(() => [ _moduleA, _moduleB ]);

        SetupSharedClassesExtra();
        _controller = CreateController();
    }
    protected abstract void SetupSharedClassesExtra();
    protected abstract void SetModule(string name);

    protected void SetAndRefreshModuleSelection(string name)
    {
        SetModule(name);
        _controller.RefreshModuleSelection();
    }
}