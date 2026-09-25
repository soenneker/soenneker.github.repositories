using System.Text.Json.Serialization;

namespace Soenneker.GitHub.Repositories;

internal sealed class SponsorshipsVariables
{
    [JsonPropertyName("repositoryId")]
    public required string RepositoryId { get; init; }

    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }
}
