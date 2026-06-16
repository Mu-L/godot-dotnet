namespace Godot.BindingsGeneration.ApiDump;

/// <summary>
/// Defines a Godot constant.
/// </summary>
public sealed class GodotConstantInfo
{
    /// <summary>
    /// Name of the constant.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Name of the type of this constant's value.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Value of the constant as a string. The value needs to be parsed and translated to proper C# syntax.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; set; }

    /// <summary>
    /// Documentation for this constant.
    /// </summary>
    /// <remarks>
    /// This is an optional field only present when <see cref="GodotApi"/> was generated with documentation included.
    /// </remarks>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <inheritdoc/>
    public override string ToString() => $"const {Type} {Name} = {Value}";
}
