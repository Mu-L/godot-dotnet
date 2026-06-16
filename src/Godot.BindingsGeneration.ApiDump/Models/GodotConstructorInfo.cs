namespace Godot.BindingsGeneration.ApiDump;

/// <summary>
/// Defines the constructor of a Godot built-in class.
/// </summary>
public sealed class GodotConstructorInfo
{
    /// <summary>
    /// Index of the constructor that must be used to invoke it.
    /// Godot built-in class may have multiple constructors so the index must be specified.
    /// </summary>
    [JsonPropertyName("index")]
    public required int Index { get; set; }

    /// <summary>
    /// Collection of argument information for the constructor.
    /// </summary>
    [JsonPropertyName("arguments")]
    public GodotArgumentInfo[] Arguments { get; set; } = [];

    /// <summary>
    /// Documentation for this constructor.
    /// </summary>
    /// <remarks>
    /// This is an optional field only present when <see cref="GodotApi"/> was generated with documentation included.
    /// </remarks>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <inheritdoc/>
    public override string ToString() => $"constructor({string.Join(", ", (object[])Arguments)})";
}
