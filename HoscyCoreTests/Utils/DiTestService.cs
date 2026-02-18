using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;

namespace HoscyCoreTests.Utils;

public interface IDiTestService : IService { }

[LoadIntoDiContainer(typeof(IDiTestService))]
public class DiTestService : IDiTestService { }
public class DiTestService2 : IDiTestService { }