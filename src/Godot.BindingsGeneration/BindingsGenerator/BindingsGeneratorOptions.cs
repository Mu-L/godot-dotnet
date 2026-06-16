using System;
using Godot.BindingsGeneration.ApiDump;

namespace Godot.BindingsGeneration;

/// <summary>
/// Options for configuring the behavior of the bindings generator.
/// </summary>
public sealed class BindingsGeneratorOptions
{
    /// <summary>
    /// The target architecture for the generated bindings.
    /// </summary>
    public ArchBits Bits { get; init; } = ArchBits.Bits64;

    /// <summary>
    /// The precision of floating-point numbers for the generated bindings.
    /// </summary>
    public GodotFloatTypePrecision FloatPrecision { get; init; } = GodotFloatTypePrecision.Single;

    /// <summary>
    /// Gets the build configuration based on the selected floating-point precision and architecture.
    /// </summary>
    public GodotBuildConfiguration BuildConfiguration =>
        (FloatPrecision, Bits) switch
        {
            (GodotFloatTypePrecision.Single, ArchBits.Bits32) => GodotBuildConfiguration.Float32,
            (GodotFloatTypePrecision.Single, ArchBits.Bits64) => GodotBuildConfiguration.Float64,
            (GodotFloatTypePrecision.Double, ArchBits.Bits32) => GodotBuildConfiguration.Double32,
            (GodotFloatTypePrecision.Double, ArchBits.Bits64) => GodotBuildConfiguration.Double64,
            _ => throw new InvalidOperationException($"Unrecognized build configuration {FloatPrecision}_{Bits}."),
        };

    /// <summary>
    /// Whether the generated bindings should include documentation comments found in the API dump.
    /// </summary>
    /// <remarks>
    /// This is enabled by default, but can be disabled if documentation is not needed or causing
    /// issues during generation. Disabling documentation can also speed up the generation process.
    /// </remarks>
    public bool IncludeDocumentation { get; init; } = true;

    /// <summary>
    /// The namespace to use for the generated API.
    /// </summary>
    /// <remarks>
    /// The <c>Godot</c> namespace is reserved for core engine API.
    /// </remarks>
    public string Namespace { get; init; } = "Godot";
}
