using Octokit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Soenneker.GitHub.Repositories.Abstract;

/// <summary>
/// A utility library for GitHub repository related operations
/// </summary>
public interface IGitHubRepositoriesUtil
{
    ValueTask<IReadOnlyList<Repository>> GetAllForOwner(string owner, CancellationToken cancellationToken = default);
}