namespace Godot.BindingsGeneration.ApiDump;

/// <summary>
/// Defines a Godot signal for an engine class.
/// </summary>
public sealed class GodotSignalInfo
{
    /// <summary>
    /// Name of the signal.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Collection of argument information for the signal.
    /// </summary>
    [JsonPropertyName("arguments")]
    public GodotArgumentInfo[] Arguments { get; set; } = [];

    /// <summary>
    /// Documentation for this signal.
    /// </summary>
    /// <remarks>
    /// This is an optional field only present when <see cref="GodotApi"/> was generated with documentation included.
    /// </remarks>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"signal {Name}({string.Join(", ", (object[])Arguments)})";
}
