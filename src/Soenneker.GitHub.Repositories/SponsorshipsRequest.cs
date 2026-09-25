using System.Text.Json.Serialization;

namespace Soenneker.GitHub.Repositories;

internal sealed class SponsorshipsRequest
{
    [JsonPropertyName("query")]
    public required string Query { get; init; }

    [JsonPropertyName("variables")]
    public required SponsorshipsVariables Variables { get; init; }
}
