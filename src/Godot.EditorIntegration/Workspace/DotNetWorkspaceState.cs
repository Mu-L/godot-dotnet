namespace Godot.EditorIntegration.Workspace;

// Values must match DotNetModule::UserWorkspaceState in dotnet_module.h.
// The C++ enum starts at 0 (PROJECT_NOT_FOUND), which the C# side never sets directly.
internal enum DotNetWorkspaceState
{
    Uninitialized = -1, // Not sent to C++; used as the initial state before loading begins.
    Loading = 1,        // Matches UserWorkspaceState::LOADING.
    Failed = 2,         // Matches UserWorkspaceState::FAILED_TO_LOAD.
    Loaded = 3,         // Matches UserWorkspaceState::LOADED.
}
