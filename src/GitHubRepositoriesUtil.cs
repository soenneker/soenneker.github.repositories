using Microsoft.Extensions.Logging;
using Soenneker.GitHub.ClientUtil.Abstract;
using Soenneker.GitHub.OpenApiClient;
using Soenneker.GitHub.OpenApiClient.Models;
using Soenneker.GitHub.OpenApiClient.Repos.Item.Item;
using Soenneker.GitHub.OpenApiClient.User.Repos;
using Soenneker.GitHub.Repositories.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.GitHub.Repositories;

/// <inheritdoc cref="IGitHubRepositoriesUtil"/>
public sealed class GitHubRepositoriesUtil : IGitHubRepositoriesUtil
{
    private readonly ILogger<GitHubRepositoriesUtil> _logger;
    private readonly IGitHubOpenApiClientUtil _gitHubClientUtil;

    public GitHubRepositoriesUtil(ILogger<GitHubRepositoriesUtil> logger, IGitHubOpenApiClientUtil gitHubClientUtil)
    {
        _logger = logger;
        _gitHubClientUtil = gitHubClientUtil;
    }

    public async ValueTask<FullRepository> Create(string name, string? description = null, bool isPrivate = false, bool autoInit = true,
        bool? allowAutoMerge = null, bool? allowMergeCommit = null, bool? allowRebaseMerge = null, bool? allowSquashMerge = null, bool? hasDiscussions = null,
        bool? deleteBranchOnMerge = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Repository name cannot be empty", nameof(name));

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken);

        var requestBody = new ReposPostRequestBody
        {
            Name = name,
            Description = description,
            Private = isPrivate,
            AutoInit = autoInit,
            AllowAutoMerge = allowAutoMerge,
            AllowMergeCommit = allowMergeCommit,
            AllowRebaseMerge = allowRebaseMerge,
            AllowSquashMerge = allowSquashMerge,
            HasDiscussions = hasDiscussions,
            DeleteBranchOnMerge = deleteBranchOnMerge
        };

        return await client.User.Repos.PostAsync(requestBody, null, cancellationToken);
    }

    public async ValueTask<FullRepository?> GetByName(string owner, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken);
            return await client.Repos[owner][name].GetAsync(cancellationToken: cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async ValueTask<List<MinimalRepository>> GetAllForOwner(string owner, DateTime? startAt = null, DateTime? endAt = null,
        CancellationToken cancellationToken = default)
    {
        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken);

        var allRepositories = new List<MinimalRepository>();
        var page = 1;
        List<MinimalRepository> repositories;

        do
        {
            repositories = await client.Users[owner]
                                       .Repos.GetAsync(requestConfiguration => requestConfiguration.QueryParameters.Page = page, cancellationToken);

            if (startAt == null && endAt == null)
            {
                allRepositories.AddRange(repositories);
            }
            else if (startAt != null && endAt == null)
            {
                allRepositories.AddRange(repositories.Where(r => r.CreatedAt >= startAt));
            }
            else if (startAt == null && endAt != null)
            {
                allRepositories.AddRange(repositories.Where(r => r.CreatedAt <= endAt));
            }
            else
            {
                allRepositories.AddRange(repositories.Where(r => r.CreatedAt >= startAt && r.CreatedAt <= endAt));
            }

            page++;
        } while (repositories.Count > 0 && !cancellationToken.IsCancellationRequested);

        return allRepositories;
    }

    /// <summary>
    /// Replaces topics for a repository
    /// </summary>
    public async ValueTask ReplaceTopics(string owner, string name, List<string> topics, CancellationToken cancellationToken = default)
    {
        if (topics?.Any() == true)
        {
            GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken);

            var requestBody = new OpenApiClient.Repos.Item.Item.Topics.TopicsPutRequestBody
            {
                Names = topics
            };

            await client.Repos[owner][name].Topics.PutAsync(requestBody, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Deletes a repository if it exists
    /// </summary>
    public async ValueTask DeleteIfExists(IGitHubOpenApiClientUtil gitHubClientUtil, string owner, string repository,
        CancellationToken cancellationToken = default)
    {
        string name = repository.ToLowerInvariant();

        if (!await DoesExistAsync(owner, name, cancellationToken))
            return;

        GitHubOpenApiClient client = await gitHubClientUtil.Get(cancellationToken);
        await client.Repos[owner][name].DeleteAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Checks if a repository exists
    /// </summary>
    public async ValueTask<bool> DoesExistAsync(string owner, string name, CancellationToken cancellationToken = default)
    {
        FullRepository? result = await GetByName(owner, name, cancellationToken);
        return result != null;
    }

    /// <summary>
    /// Toggles auto-merge for a repository
    /// </summary>
    public async ValueTask ToggleAutoMergeAsync(string owner, string name, bool enable, CancellationToken cancellationToken = default)
    {
        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken);

        var requestBody = new RepoPatchRequestBody
        {
            AllowAutoMerge = enable
        };

        await client.Repos[owner][name].PatchAsync(requestBody, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Toggles auto-merge for all repositories of an owner
    /// </summary>
    public async ValueTask ToggleAutoMergeOnAllRepos(string owner, bool enable, DateTime? startAt = null, DateTime? endAt = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MinimalRepository> repositories = await GetAllForOwner(owner, startAt, endAt, cancellationToken);

        if (repositories?.Any() != true)
            return;

        foreach (MinimalRepository repo in repositories)
        {
            try
            {
                await ToggleAutoMergeAsync(owner, repo.Name, enable, cancellationToken);
            }
            catch
            {
                // Ignore errors for individual repositories
            }
        }
    }
}