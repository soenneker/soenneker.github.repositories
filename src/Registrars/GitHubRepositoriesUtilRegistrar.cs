using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.GitHub.Client.Registrars;
using Soenneker.GitHub.Repositories.Abstract;

namespace Soenneker.GitHub.Repositories.Registrars;

/// <summary>
/// A utility library for GitHub repository related operations
/// </summary>
public static class GitHubRepositoriesUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IGitHubRepositoriesUtil"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddGitHubRepositoriesUtilAsSingleton(this IServiceCollection services)
    {
        services.AddGitHubClientUtilAsSingleton()
                .TryAddSingleton<IGitHubRepositoriesUtil, GitHubRepositoriesUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IGitHubRepositoriesUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddGitHubRepositoriesUtilAsScoped(this IServiceCollection services)
    {
        services.AddGitHubClientUtilAsSingleton()
                .TryAddScoped<IGitHubRepositoriesUtil, GitHubRepositoriesUtil>();

        return services;
    }
}