using Octokit;
using Soenneker.GitHub.Repositories.Abstract;
using System.Collections.Generic;
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

        Repository? result = await (await _gitHubClientUtil.Get(cancellationToken).NoSync()).Repository.Create(repository).NoSync();

        return result;
    }

    public async ValueTask<Repository?> GetByName(string owner, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            Repository? result = await (await _gitHubClientUtil.Get(cancellationToken).NoSync()).Repository.Get(owner, name).NoSync();
            return result;
        }
        catch (ApiException)
        {
            return null;
        }
    }

    public async ValueTask<IReadOnlyList<Repository>> GetAllForOwner(string owner, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all repositories for owner ({owner})...", owner);

        IReadOnlyList<Repository>? repositories = await (await _gitHubClientUtil.Get(cancellationToken).NoSync()).Repository.GetAllForUser(owner).NoSync();

        return repositories;
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

    public async ValueTask AllowAutoMerge(string owner, string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting GitHub repository ({owner}/{name}) auto merge allow...", name);

        var update = new RepositoryUpdate
        {
            AllowAutoMerge = true
        };

        Repository? _ = await (await _gitHubClientUtil.Get(cancellationToken).NoSync()).Repository.Edit(owner, name, update).NoSync();
    }

    public async ValueTask AllowAutoMergeOnAllRepos(string owner, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Repository> repositories = await GetAllForOwner(owner, cancellationToken).NoSync();

        if (repositories.IsNullOrEmpty())
            return;

        foreach (Repository repo in repositories)
        {
            try
            {
                await AllowAutoMerge(owner, repo.Name, cancellationToken).NoSync();
            }
            catch
            {
            }
        }
    }
}