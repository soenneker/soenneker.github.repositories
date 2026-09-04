using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Object;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.GitHub.Client.Http.Abstract;
using Soenneker.GitHub.ClientUtil.Abstract;
using Soenneker.GitHub.OpenApiClient;
using Soenneker.GitHub.OpenApiClient.Models;
using Soenneker.GitHub.Repositories.Abstract;

namespace Soenneker.GitHub.Repositories;

/// <inheritdoc cref="IGitHubRepositoriesUtil" />
public sealed class GitHubRepositoriesUtil : IGitHubRepositoriesUtil
{
    private readonly ILogger<GitHubRepositoriesUtil> _logger;
    private readonly IGitHubOpenApiClientUtil _gitHubClientUtil;
    private readonly IGitHubHttpClient _gitHubHttpClient;

    public GitHubRepositoriesUtil(ILogger<GitHubRepositoriesUtil> logger, IGitHubOpenApiClientUtil gitHubClientUtil, IGitHubHttpClient gitHubHttpClient)
    {
        _logger = logger;
        _gitHubClientUtil = gitHubClientUtil;
        _gitHubHttpClient = gitHubHttpClient;
    }

    public ValueTask<FullRepository> Create(string name, string? description = null, bool isPrivate = false, bool? allowAutoMerge = null,
        bool? allowMergeCommit = null, bool? allowRebaseMerge = null, bool? allowSquashMerge = null, bool? hasDiscussions = null, string? homepage = null,
        bool? hasWiki = null, bool? hasDownloads = null, bool? hasProjects = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user repository: {Name}, Private: {IsPrivate}", name, isPrivate);

        var requestBody = new ReposCreateForAuthenticatedUserRequest
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

    public async ValueTask<FullRepository> Create(ReposCreateForAuthenticatedUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending user repository creation request for: {Repo}", request.Name);
        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken)
                                                            .NoSync();
        return await client.User.Repos.PostAsync(request, null, cancellationToken)
                           .NoSync() ?? throw new InvalidOperationException("GitHub returned no repository after creating it.");
    }

    public async ValueTask<FullRepository> CreateForOrg(string org, string name, string? description = null, bool isPrivate = false,
        bool? allowAutoMerge = null, bool? allowMergeCommit = null, bool? allowRebaseMerge = null, bool? allowSquashMerge = null, string? homepage = null,
        bool? hasWiki = null, bool? hasDownloads = null, bool? hasProjects = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating org repository: {Org}/{Name}, Private: {IsPrivate}", org, name, isPrivate);

        var requestBody = new ReposCreateInOrgRequest
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

        return await CreateForOrg(org, requestBody, cancellationToken)
            .NoSync();
    }

    public async ValueTask<FullRepository> CreateForOrg(string org, ReposCreateInOrgRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending org repository creation request for: {Org}/{Repo}", org, request.Name);
        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken)
                                                            .NoSync();
        return await client.Orgs[org]
                           .Repos.PostAsync(request, null, cancellationToken)
                           .NoSync() ?? throw new InvalidOperationException($"GitHub returned no repository after creating {org}/{request.Name}.");
    }

    public async ValueTask<FullRepository?> GetByName(string owner, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching repository: {Owner}/{Name}", owner, name);
            GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken)
                                                                .NoSync();
            return await client.Repos[owner][name]
                               .GetAsync(cancellationToken: cancellationToken)
                               .NoSync();
        }
        catch (BasicError ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogDebug("Repository not found: {Owner}/{Name}", owner, name);
            return null;
        }
    }

    public async ValueTask<List<MinimalRepository>> GetAllForOwner(string owner, DateTimeOffset? startAt = null, DateTimeOffset? endAt = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all repositories for owner: {Owner}, Start: {Start}, End: {End}", owner, startAt, endAt);

        var allRepositories = new List<MinimalRepository>();

        await foreach (MinimalRepository repository in GetAllForOwnerIncrementally(owner, startAt, endAt, cancellationToken: cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            allRepositories.Add(repository);
        }

        _logger.LogInformation("Fetched {Count} repositories for {Owner}", allRepositories.Count, owner);
        return allRepositories;
    }

    public async IAsyncEnumerable<MinimalRepository> GetAllForOwnerIncrementally(string owner, DateTimeOffset? startAt = null, DateTimeOffset? endAt = null,
        int pageSize = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than 0.");

        _logger.LogInformation("Getting repositories incrementally for owner: {Owner}, Start: {Start}, End: {End}", owner, startAt, endAt);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken)
                                                            .NoSync();

        var page = 1;
        bool useDateFilter = startAt != null || endAt != null;

        var done = false;

        while (!cancellationToken.IsCancellationRequested && !done)
        {
            int localPage = page;

            List<MinimalRepository>? repositories = await client.Users[owner]
                                                                .Repos.GetAsync(requestConfiguration =>
                                                                {
                                                                    requestConfiguration.QueryParameters.Page = localPage;
                                                                    requestConfiguration.QueryParameters.PerPage = pageSize;

                                                                    if (useDateFilter)
                                                                    {
                                                                        requestConfiguration.QueryParameters.Sort = ReposListForUserSortParameter.Created;
                                                                        requestConfiguration.QueryParameters.Direction = ReposListForUserDirectionParameter.Desc;
                                                                    }
                                                                }, cancellationToken)
                                                                .NoSync();

            if (repositories == null || repositories.Count == 0)
                break;

            foreach (MinimalRepository r in repositories)
            {
                if (startAt != null && r.CreatedAt < startAt)
                {
                    if (useDateFilter)
                    {
                        done = true;
                        break;
                    }

                    continue;
                }

                if (endAt != null && r.CreatedAt > endAt)
                {
                    if (!useDateFilter)
                    {
                        done = true;
                        break;
                    }

                    continue;
                }

                yield return r;
            }

            if (repositories.Count < pageSize)
                break;

            if (useDateFilter && startAt != null && repositories[^1].CreatedAt < startAt)
                break;

            page++;
        }
    }

    public async ValueTask ReplaceTopics(string owner, string name, List<string> topics, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Replacing topics for repository: {Owner}/{Name}", owner, name);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken)
                                                            .NoSync();
        var requestBody = new ReposReplaceAllTopicsRequest
        {
            Names = topics
        };

        await client.Repos[owner][name]
                    .Topics.PutAsync(requestBody, cancellationToken: cancellationToken)
                    .NoSync();
    }

    public async ValueTask DeleteIfExists(string owner, string repository, CancellationToken cancellationToken = default)
    {
        string name = repository.ToLowerInvariantFast();
        if (!await DoesExist(owner, name, cancellationToken)
                .NoSync())
        {
            _logger.LogInformation("Repository does not exist: {Owner}/{Name}", owner, name);
            return;
        }

        _logger.LogInformation("Deleting repository: {Owner}/{Name}", owner, name);
        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken)
                                                            .NoSync();
        await client.Repos[owner][name]
                    .DeleteAsync(cancellationToken: cancellationToken)
                    .NoSync();
    }

    public async ValueTask<bool> DoesExist(string owner, string name, CancellationToken cancellationToken = default)
    {
        FullRepository? result = await GetByName(owner, name, cancellationToken)
            .NoSync();
        bool exists = result != null;
        _logger.LogDebug("Checked existence of {Owner}/{Name}: {Exists}", owner, name, exists);
        return exists;
    }

    public async ValueTask ToggleAutoMerge(string owner, string name, bool enable, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Toggling auto-merge for {Owner}/{Name}: {Enabled}", owner, name, enable);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken)
                                                            .NoSync();
        var requestBody = new ReposUpdateRequest
        {
            AllowAutoMerge = enable
        };

        await client.Repos[owner][name]
                    .PatchAsync(requestBody, cancellationToken: cancellationToken)
                    .NoSync();
    }

    public async ValueTask ToggleSponsorships(string owner, string name, bool enable, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Toggling sponsorships for {Owner}/{Name}: {Enabled}", owner, name, enable);

        FullRepository? repository = await GetByName(owner, name, cancellationToken)
            .NoSync();
        string repositoryId = repository?.NodeId ??
                              throw new InvalidOperationException($"GitHub did not return a node ID for {owner}/{name}.");

        var payload = new
        {
            query =
                "mutation($repositoryId:ID!,$enabled:Boolean!){updateRepository(input:{repositoryId:$repositoryId,hasSponsorshipsEnabled:$enabled}){repository{hasSponsorshipsEnabled}}}",
            variables = new {repositoryId, enabled = enable}
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "graphql");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Add("User-Agent", "soenneker.github.repositories");
        request.Content = payload.ToHttpContent();

        HttpClient client = await _gitHubHttpClient.Get(cancellationToken)
                                                   .NoSync();
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken)
                                                         .NoSync();
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(cancellationToken)
                                            .NoSync();
        using JsonDocument document = JsonDocument.Parse(json);

        if (document.RootElement.TryGetProperty("errors", out JsonElement errors) && errors.GetArrayLength() > 0)
            throw new InvalidOperationException($"GitHub GraphQL request failed: {errors}");

        bool actual = document.RootElement.GetProperty("data")
                                     .GetProperty("updateRepository")
                                     .GetProperty("repository")
                                     .GetProperty("hasSponsorshipsEnabled")
                                     .GetBoolean();

        if (actual != enable)
            throw new InvalidOperationException($"GitHub did not set sponsorships to {enable} for {owner}/{name}.");
    }

    public async ValueTask ToggleAutoMergeOnAllRepos(string owner, bool enable, DateTimeOffset? startAt = null, DateTimeOffset? endAt = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Toggling auto-merge on all repositories for {Owner}. Enable: {Enable}", owner, enable);

        IReadOnlyList<MinimalRepository> repositories = await GetAllForOwner(owner, startAt, endAt, cancellationToken)
            .NoSync();

        if (repositories.Count == 0)
        {
            _logger.LogWarning("No repositories found for auto-merge toggle: {Owner}", owner);
            return;
        }

        var failures = new List<Exception>();

        foreach (MinimalRepository repo in repositories)
        {
            try
            {
                if (repo.Name == null)
                    continue;

                await ToggleAutoMerge(owner, repo.Name, enable, cancellationToken)
                    .NoSync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to toggle auto-merge on: {Repo}", repo.Name);
                failures.Add(ex);
            }
        }

        if (failures.Count > 0)
            throw new AggregateException($"Failed to update auto-merge on {failures.Count} repository/repositories for {owner}.", failures);
    }

    public async ValueTask<string> CreateUnique(string owner, string baseName, string? description = null, bool isPrivate = false, bool? allowAutoMerge = null,
        bool? allowMergeCommit = null, bool? allowRebaseMerge = null, bool? allowSquashMerge = null, string? homepage = null, bool? hasWiki = null,
        bool? hasDownloads = null, bool? hasProjects = null, CancellationToken cancellationToken = default)
    {
        string candidate = baseName;
        var counter = 1;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool exists = await DoesExist(owner, candidate, cancellationToken)
                .NoSync();
            if (!exists)
            {
                _logger.LogInformation("Creating unique org repo: {Org}/{Name}", owner, candidate);
                await CreateForOrg(owner, candidate, description, isPrivate, allowAutoMerge, allowMergeCommit, allowRebaseMerge, allowSquashMerge, homepage,
                        hasWiki, hasDownloads, hasProjects, cancellationToken)
                    .NoSync();

                return candidate;
            }

            candidate = $"{baseName}-{counter++}";
        }
    }
}
