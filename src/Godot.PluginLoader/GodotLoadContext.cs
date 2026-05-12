using System.Reflection;
using System.Runtime.Loader;

namespace Godot.PluginLoader;

/// <summary>
/// Implements an ALC to be used by Godot when loading .NET assemblies using hostfxr
/// which allows unloading the .NET assemblies when the GDExtension needs to be reloaded.
/// </summary>
internal sealed class GodotLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public GodotLoadContext(string mainAssemblyPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (!string.IsNullOrEmpty(assemblyPath))
        {
            // Load dependency assemblies from their file path so that Assembly.Location is
            // set correctly. Some libraries (e.g. Microsoft.CodeAnalysis.Workspaces.MSBuild)
            // rely on Assembly.Location to locate their own side-by-side resources at runtime.
            // Unlike the main assembly (which is loaded from a stream in Main.cs to avoid
            // file-locking on Windows), dependencies are probably fine to load from path.
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        string? unmanagedDllPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (!string.IsNullOrEmpty(unmanagedDllPath))
        {
            return LoadUnmanagedDllFromPath(unmanagedDllPath);
        }

        return 0;
    }
}
