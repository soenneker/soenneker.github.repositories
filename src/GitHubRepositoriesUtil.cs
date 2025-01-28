using System;
using Octokit;
using Soenneker.GitHub.Repositories.Abstract;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Enumerable;
using Soenneker.Extensions.Enumerable.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.GitHub.Client.Abstract;
using Soenneker.Extensions.String;

namespace Soenneker.GitHub.Repositories;

/// <inheritdoc cref="IGitHubRepositoriesUtil"/>
public class GitHubRepositoriesUtil : IGitHubRepositoriesUtil
{
    private readonly ILogger<GitHubRepositoriesUtil> _logger;
    private readonly IGitHubClientUtil _gitHubClientUtil;

    public GitHubRepositoriesUtil(ILogger<GitHubRepositoriesUtil> logger, IGitHubClientUtil gitHubClientUtil)
    {
        _logger = logger;
        _gitHubClientUtil = gitHubClientUtil;
    }

    public async ValueTask<Repository?> Create(NewRepository repository, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating GitHub repository ({name})...", repository.Name);

        return await (await _gitHubClientUtil.Get(cancellationToken).NoSync()).Repository.Create(repository).NoSync();
    }

    public async ValueTask<Repository?> GetByName(string owner, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            return await (await _gitHubClientUtil.Get(cancellationToken).NoSync()).Repository.Get(owner, name).NoSync();
        }
        catch (ApiException)
        {
            return null;
        }
    }

    public async ValueTask<IReadOnlyList<Repository>> GetAllForOwner(string owner, DateTime? startAt = null, DateTime? endAt = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all repositories for owner ({owner})...", owner);

        GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        var allRepositories = new List<Repository>();
        var page = 1;
        IReadOnlyList<Repository> repositories;

        do
        {
            var options = new ApiOptions
            {
                PageCount = 1,
                PageSize = 100, // GitHub API default max page size
                StartPage = page
            };

            repositories = await client.Repository.GetAllForUser(owner, options).NoSync();

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

        _logger.LogDebug("All repositories for owner ({owner}) have been retrieved", owner);

        return allRepositories;
    }

    public async ValueTask ReplaceTopics(string owner, string name, List<string> topics, CancellationToken cancellationToken = default)
    {
        if (topics.Populated())
        {
            _logger.LogInformation("Replacing topics: {topics}...", topics.ToCommaSeparatedString());

            var repositoryTopics = new RepositoryTopics(topics);

            await (await _gitHubClientUtil.Get(cancellationToken).NoSync()).Repository.ReplaceAllTopics(owner, name, repositoryTopics).NoSync();
        }
    }

    public async ValueTask DeleteIfExists(string owner, string repository, CancellationToken cancellationToken = default)
    {
        string name = repository.ToLowerInvariantFast();

        if (!await DoesExist(owner, name, cancellationToken).NoSync())
            return;

        _logger.LogInformation("Deleting GitHub repository {name}...", name);

        await (await _gitHubClientUtil.Get(cancellationToken).NoSync()).Repository.Delete(owner, name).NoSync();
    }

    public async ValueTask<bool> DoesExist(string owner, string name, CancellationToken cancellationToken = default)
    {
        Repository? result = await GetByName(owner, name, cancellationToken).NoSync();

        if (result != null)
        {
            _logger.LogInformation("GitHub repository ({name}) exists", name);
            return true;
        }

        _logger.LogInformation("GitHub repository ({name}) does not exist", name);
        return false;
    }

    public async ValueTask ToggleAutoMerge(string owner, string name, bool enable, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting GitHub repository ({owner}/{name}) auto merge {enable}...", owner, name, enable);

        var update = new RepositoryUpdate
        {
            AllowAutoMerge = enable
        };

        Repository? _ = await (await _gitHubClientUtil.Get(cancellationToken).NoSync()).Repository.Edit(owner, name, update).NoSync();
    }

    public async ValueTask ToggleDiscussions(string owner, string name, bool enable, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting GitHub repository ({owner}/{name}) discussions {enable}...", owner, name, enable);

        var update = new RepositoryUpdate
        {
            HasDiscussions = enable
        };

        Repository? _ = await (await _gitHubClientUtil.Get(cancellationToken).NoSync()).Repository.Edit(owner, name, update).NoSync();
    }

    public async ValueTask ToggleAutoMergeOnAllRepos(string owner, bool enable, DateTime? startAt = null, DateTime? endAt = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Repository> repositories = await GetAllForOwner(owner, startAt, endAt, cancellationToken).NoSync();

        if (repositories.IsNullOrEmpty())
            return;

        foreach (Repository repo in repositories)
        {
            try
            {
                await ToggleAutoMerge(owner, repo.Name, enable, cancellationToken).NoSync();
            }
            catch
            {
            }
        }
    }

    public async ValueTask ToggleDiscussionsOnAllRepos(string owner, bool enable, DateTime? startAt = null, DateTime? endAt = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Repository> repositories = await GetAllForOwner(owner, startAt, endAt, cancellationToken).NoSync();

        if (repositories.IsNullOrEmpty())
            return;

        foreach (Repository repo in repositories)
        {
            try
            {
                await ToggleDiscussions(owner, repo.Name, enable, cancellationToken).NoSync();
            }
            catch
            {
            }
        }
    }
}