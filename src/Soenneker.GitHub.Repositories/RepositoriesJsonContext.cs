using System.Text.Json.Serialization;

namespace Soenneker.GitHub.Repositories;

[JsonSerializable(typeof(SponsorshipsRequest))]
internal partial class RepositoriesJsonContext : JsonSerializerContext
{
}
