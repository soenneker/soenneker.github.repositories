using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.GitHub.ClientUtil.Registrars;
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
        services.AddGitHubOpenApiClientUtilAsSingleton()
                .TryAddSingleton<IGitHubRepositoriesUtil, GitHubRepositoriesUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IGitHubRepositoriesUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddGitHubRepositoriesUtilAsScoped(this IServiceCollection services)
    {
        services.AddGitHubOpenApiClientUtilAsSingleton()
                .TryAddScoped<IGitHubRepositoriesUtil, GitHubRepositoriesUtil>();

        return services;
    }
}