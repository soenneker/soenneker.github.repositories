using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.GitHub.Repositories.Abstract;
using Soenneker.GitHub.Repositories.PullRequests.Registrars;

namespace Soenneker.GitHub.Repositories.Registrars;

/// <summary>
/// A utility library for GitHub repository related operations
/// </summary>
public static class GitHubRepositoriesUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IGitHubRepositoriesUtil"/> as a singleton service. <para/>
    /// </summary>
    public static void AddGitHubRepositoriesUtilAsSingleton(this IServiceCollection services)
    {
        services.AddGitHubRepositoriesPullRequestsUtilAsSingleton();
        services.TryAddSingleton<IGitHubRepositoriesUtil, GitHubRepositoriesUtil>();
    }

    /// <summary>
    /// Adds <see cref="IGitHubRepositoriesUtil"/> as a scoped service. <para/>
    /// </summary>
    public static void AddGitHubRepositoriesUtilAsScoped(this IServiceCollection services)
    {
        services.AddGitHubRepositoriesPullRequestsUtilAsScoped();
        services.TryAddScoped<IGitHubRepositoriesUtil, GitHubRepositoriesUtil>();
    }
}