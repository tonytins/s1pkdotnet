using System.Text.Json.Serialization;

namespace Sims1Pkg;

/// <summary>
/// Data Transfer Object for package metadata.
/// Used for JSON deserialization from metadata.json files.
/// </summary>
public class PackageMetadata : ISMetadata
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    // Optional fields for future expansion
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
