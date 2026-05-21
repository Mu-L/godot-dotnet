namespace Godot.EditorIntegration;

// Must be kept in sync with the C++ 'InitState' enum in 'dotnet_module_state.h'.
internal enum InitState
{
    Uninitialized,
    Initializing,
    Initialized,
    Failed,
}

// Must be kept in sync with the C++ 'AssemblyLoadFailedState' enum in 'dotnet_module_state.h'.
internal enum AssemblyLoadFailedState
{
    None,
    ProjectNotFound,
    DllNotFound,
    FailedToLoad,
    FailedToLoad_HostFxr,
    FailedToLoad_PluginLoader,
    FailedToLoad_GDExtensionEntryPoint,
    FailedToLoad_GDExtensionInit,
}

// This enum merges the 'InitState' and 'AssemblyLoadFailState' enums from the C++ side,
// to represent all possible states in a single value.
// Note that errors related to hostfxr or the plugin loader are not represented here,
// because those would prevent loading this assembly as well.
internal enum AssemblyLoadState
{
    NotLoaded,
    Loading,
    Loaded,
    ProjectNotFound,
    DllNotFound,
    FailedToLoad,
    FailedToResolveGDExtensionEntryPoint,
    FailedToInitializeGDExtension,
}
