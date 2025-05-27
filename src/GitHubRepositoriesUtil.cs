using Microsoft.Extensions.Logging;
using Soenneker.Extensions.String;
using Soenneker.GitHub.ClientUtil.Abstract;
using Soenneker.GitHub.OpenApiClient.Models;
using Soenneker.GitHub.OpenApiClient.Repos.Item.Item;
using Soenneker.GitHub.Repositories.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.GitHub.OpenApiClient;
using Soenneker.GitHub.OpenApiClient.User.Repos;

///<inheritdoc cref="IGitHubRepositoriesUtil"/>
public sealed class GitHubRepositoriesUtil : IGitHubRepositoriesUtil
{
    private readonly ILogger<GitHubRepositoriesUtil> _logger;
    private readonly IGitHubOpenApiClientUtil _gitHubClientUtil;

    public GitHubRepositoriesUtil(ILogger<GitHubRepositoriesUtil> logger, IGitHubOpenApiClientUtil gitHubClientUtil)
    {
        _logger = logger;
        _gitHubClientUtil = gitHubClientUtil;
    }

    public ValueTask<FullRepository> Create(string name, string? description = null, bool isPrivate = false,
        bool? allowAutoMerge = null, bool? allowMergeCommit = null, bool? allowRebaseMerge = null, bool? allowSquashMerge = null, bool? hasDiscussions = null,
        string? homepage = null, bool? hasWiki = null, bool? hasDownloads = null, bool? hasProjects = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user repository: {Name}, Private: {IsPrivate}", name, isPrivate);

        var requestBody = new ReposPostRequestBody
        {
            Name = name,
            Description = description,
            Private = isPrivate,
            Homepage = homepage,
            HasWiki = hasWiki,
            HasDownloads = hasDownloads,
            AllowAutoMerge = allowAutoMerge,
            AllowMergeCommit = allowMergeCommit,
            AllowRebaseMerge = allowRebaseMerge,
            AllowSquashMerge = allowSquashMerge,
            HasDiscussions = hasDiscussions,
            HasProjects = hasProjects
        };

        return Create(requestBody, cancellationToken);
    }

    public async ValueTask<FullRepository> Create(ReposPostRequestBody request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending user repository creation request for: {Repo}", request.Name);
        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();
        return await client.User.Repos.PostAsync(request, null, cancellationToken).NoSync();
    }

    public async ValueTask<FullRepository> CreateForOrg(string org, string name, string? description = null, bool isPrivate = false,
        bool? allowAutoMerge = null, bool? allowMergeCommit = null, bool? allowRebaseMerge = null, bool? allowSquashMerge = null,
        string? homepage = null, bool? hasWiki = null, bool? hasDownloads = null, bool? hasProjects = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating org repository: {Org}/{Name}, Private: {IsPrivate}", org, name, isPrivate);

        var requestBody = new Soenneker.GitHub.OpenApiClient.Orgs.Item.Repos.ReposPostRequestBody
        {
            Name = name,
            Description = description,
            Private = isPrivate,
            Homepage = homepage,
            HasWiki = hasWiki,
            HasDownloads = hasDownloads,
            AllowAutoMerge = allowAutoMerge,
            AllowMergeCommit = allowMergeCommit,
            AllowRebaseMerge = allowRebaseMerge,
            AllowSquashMerge = allowSquashMerge,
            HasProjects = hasProjects
        };

        return await CreateForOrg(org, requestBody, cancellationToken);
    }

    public async ValueTask<FullRepository> CreateForOrg(string org, Soenneker.GitHub.OpenApiClient.Orgs.Item.Repos.ReposPostRequestBody request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending org repository creation request for: {Org}/{Repo}", org, request.Name);
        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();
        return await client.Orgs[org].Repos.PostAsync(request, null, cancellationToken).NoSync();
    }

    public async ValueTask<FullRepository?> GetByName(string owner, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching repository: {Owner}/{Name}", owner, name);
            GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();
            return await client.Repos[owner][name].GetAsync(cancellationToken: cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Repository not found or failed to fetch: {Owner}/{Name}", owner, name);
            return null;
        }
    }

    public async ValueTask<List<MinimalRepository>> GetAllForOwner(string owner, DateTime? startAt = null, DateTime? endAt = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all repositories for owner: {Owner}, Start: {Start}, End: {End}", owner, startAt, endAt);
        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        var allRepositories = new List<MinimalRepository>();
        var page = 1;
        List<MinimalRepository> repositories;

        do
        {
            repositories = await client.Users[owner].Repos.GetAsync(requestConfiguration => requestConfiguration.QueryParameters.Page = page, cancellationToken).NoSync();

            IEnumerable<MinimalRepository> filtered = repositories;

            if (startAt != null)
                filtered = filtered.Where(r => r.CreatedAt >= startAt);

            if (endAt != null)
                filtered = filtered.Where(r => r.CreatedAt <= endAt);

            allRepositories.AddRange(filtered);

            page++;
        } while (repositories.Count > 0 && !cancellationToken.IsCancellationRequested);

        _logger.LogInformation("Fetched {Count} repositories for {Owner}", allRepositories.Count, owner);
        return allRepositories;
    }

    public async ValueTask ReplaceTopics(string owner, string name, List<string> topics, CancellationToken cancellationToken = default)
    {
        if (topics?.Any() != true)
        {
            _logger.LogWarning("No topics provided for replacement in: {Owner}/{Name}", owner, name);
            return;
        }

        _logger.LogInformation("Replacing topics for repository: {Owner}/{Name}", owner, name);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();
        var requestBody = new Soenneker.GitHub.OpenApiClient.Repos.Item.Item.Topics.TopicsPutRequestBody
        {
            Names = topics
        };

        await client.Repos[owner][name].Topics.PutAsync(requestBody, cancellationToken: cancellationToken).NoSync();
    }

    public async ValueTask DeleteIfExists(string owner, string repository, CancellationToken cancellationToken = default)
    {
        string name = repository.ToLowerInvariantFast();
        if (!await DoesExistAsync(owner, name, cancellationToken).NoSync())
        {
            _logger.LogInformation("Repository does not exist: {Owner}/{Name}", owner, name);
            return;
        }

        _logger.LogInformation("Deleting repository: {Owner}/{Name}", owner, name);
        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();
        await client.Repos[owner][name].DeleteAsync(cancellationToken: cancellationToken).NoSync();
    }

    public async ValueTask<bool> DoesExistAsync(string owner, string name, CancellationToken cancellationToken = default)
    {
        FullRepository? result = await GetByName(owner, name, cancellationToken).NoSync();
        bool exists = result != null;
        _logger.LogDebug("Checked existence of {Owner}/{Name}: {Exists}", owner, name, exists);
        return exists;
    }

    public async ValueTask ToggleAutoMergeAsync(string owner, string name, bool enable, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Toggling auto-merge for {Owner}/{Name}: {Enabled}", owner, name, enable);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();
        var requestBody = new RepoPatchRequestBody
        {
            AllowAutoMerge = enable
        };

        await client.Repos[owner][name].PatchAsync(requestBody, cancellationToken: cancellationToken).NoSync();
    }

    public async ValueTask ToggleAutoMergeOnAllRepos(string owner, bool enable, DateTime? startAt = null, DateTime? endAt = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Toggling auto-merge on all repositories for {Owner}. Enable: {Enable}", owner, enable);

        IReadOnlyList<MinimalRepository> repositories = await GetAllForOwner(owner, startAt, endAt, cancellationToken).NoSync();

        if (repositories?.Any() != true)
        {
            _logger.LogWarning("No repositories found for auto-merge toggle: {Owner}", owner);
            return;
        }

        foreach (MinimalRepository repo in repositories)
        {
            try
            {
                await ToggleAutoMergeAsync(owner, repo.Name, enable, cancellationToken).NoSync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to toggle auto-merge on: {Repo}", repo.Name);
            }
        }
    }
}
