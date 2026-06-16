namespace Godot.BindingsGeneration.ApiDump;

/// <summary>
/// Defines the member of a Godot built-in class.
/// </summary>
public sealed class GodotMemberInfo
{
    /// <summary>
    /// Name of the member.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Name of the type of the member.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Documentation for this member.
    /// </summary>
    /// <remarks>
    /// This is an optional field only present when <see cref="GodotApi"/> was generated with documentation included.
    /// </remarks>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <inheritdoc/>
    public override string ToString() => $"{Type} {Name}";
}
