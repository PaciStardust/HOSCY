using HoscyCli.Commands.Core;
using HoscyCore.Services.DependencyCore;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(FieldModificatorModule), Lifetime.Singleton)]
public class FieldModificatorModule : AttributeCommandModule
{
    
}

/// Todo: Redesign of vairable setting
/// Following should be handled:
/// all methods should always take and get / set the direct config value on top level
/// base types should be handled instantly, complex values should be split up and then handled and then finally list/sets/dicts also need their editor
/// if possible current states should always get a readout so there should not only be a "set/get" but also "editor" system
/// preferably the whole system should be fairly independent of the actual type