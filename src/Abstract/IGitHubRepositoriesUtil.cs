using Soenneker.GitHub.OpenApiClient.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.GitHub.OpenApiClient.User.Repos;

namespace Soenneker.GitHub.Repositories.Abstract;

/// <summary>
/// A utility library for GitHub repository related operations
/// </summary>
public interface IGitHubRepositoriesUtil
{
    /// <summary>
    /// Creates a new GitHub repository for the authenticated user.
    /// </summary>
    ValueTask<FullRepository> Create(string name, string? description = null, bool isPrivate = false, bool? allowAutoMerge = null,
        bool? allowMergeCommit = null, bool? allowRebaseMerge = null, bool? allowSquashMerge = null, bool? hasDiscussions = null, string? homepage = null,
        bool? hasWiki = null, bool? hasDownloads = null, bool? hasProjects = null, CancellationToken cancellationToken = default);

    ValueTask<FullRepository> Create(ReposPostRequestBody request, CancellationToken cancellationToken = default);

    ValueTask<FullRepository> CreateForOrg(string org, string name, string? description = null, bool isPrivate = false, bool? allowAutoMerge = null,
        bool? allowMergeCommit = null, bool? allowRebaseMerge = null, bool? allowSquashMerge = null, string? homepage = null, bool? hasWiki = null,
        bool? hasDownloads = null, bool? hasProjects = null, CancellationToken cancellationToken = default);

    ValueTask<FullRepository> CreateForOrg(string org, OpenApiClient.Orgs.Item.Repos.ReposPostRequestBody request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a repository by owner and name.
    /// </summary>
    ValueTask<FullRepository?> GetByName(string owner, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all repositories for the specified owner, optionally filtered by creation date.
    /// </summary>
    ValueTask<List<MinimalRepository>> GetAllForOwner(string owner, DateTime? startAt = null, DateTime? endAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the topics of a repository.
    /// </summary>
    ValueTask ReplaceTopics(string owner, string name, List<string> topics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a repository if it exists.
    /// </summary>
    ValueTask DeleteIfExists(string owner, string repository, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a repository exists.
    /// </summary>
    ValueTask<bool> DoesExistAsync(string owner, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables auto-merge for a repository.
    /// </summary>
    ValueTask ToggleAutoMergeAsync(string owner, string name, bool enable, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables auto-merge for all repositories of an owner, optionally filtered by creation date.
    /// </summary>
    ValueTask ToggleAutoMergeOnAllRepos(string owner, bool enable, DateTime? startAt = null, DateTime? endAt = null,
        CancellationToken cancellationToken = default);
}