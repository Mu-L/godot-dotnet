using System;
using Godot.BindingsGeneration.ApiDump;

namespace Godot.BindingsGeneration;

internal sealed class BindingsGeneratorOptions
{
    /// <summary>
    /// When version check is enabled, the generator will validate that the API version matches
    /// the version of the Godot .NET packages. This can help catch configuration issues where
    /// an outdated or incorrect 'extension_api.json' file is being used. However, users can
    /// keep it disabled if they need to force generation for a custom or mismatched 'extension_api.json'
    /// file that may still be compatible.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false"/> (version checking is disabled by default).
    /// </remarks>
    public bool ValidateBindingsVersion { get; init; }

    public enum ArchBits { Bits32, Bits64 };

    public ArchBits Bits { get; init; } = ArchBits.Bits64;

    public enum FloatTypePrecision
    {
        SinglePrecision = GodotFloatTypePrecision.Single,
        DoublePrecision = GodotFloatTypePrecision.Double,
    };

    public FloatTypePrecision FloatPrecision { get; init; } = FloatTypePrecision.SinglePrecision;

    public GodotBuildConfiguration BuildConfiguration =>
        (FloatPrecision, Bits) switch
        {
            (FloatTypePrecision.SinglePrecision, ArchBits.Bits32) => GodotBuildConfiguration.Float32,
            (FloatTypePrecision.SinglePrecision, ArchBits.Bits64) => GodotBuildConfiguration.Float64,
            (FloatTypePrecision.DoublePrecision, ArchBits.Bits32) => GodotBuildConfiguration.Double32,
            (FloatTypePrecision.DoublePrecision, ArchBits.Bits64) => GodotBuildConfiguration.Double64,
            _ => throw new InvalidOperationException($"Unrecognized build configuration {FloatPrecision}_{Bits}."),
        };
}
