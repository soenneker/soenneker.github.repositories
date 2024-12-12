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
    ValueTask<Repository?> Create(NewRepository repository, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a GitHub repository by its owner and name.
    /// </summary>
    /// <param name="owner">The username or organization name of the repository owner.</param>
    /// <param name="name">The name of the repository.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{Repository}"/> representing the asynchronous operation. 
    /// The result contains the repository if found; otherwise, <c>null</c>.
    /// </returns>
    ValueTask<Repository?> GetByName(string owner, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all repositories for a specified owner.
    /// </summary>
    /// <param name="owner">The username or organization name of the repository owner.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{IReadOnlyList{Repository}}"/> containing the list of repositories owned by the specified owner.
    /// </returns>
    ValueTask<IReadOnlyList<Repository>> GetAllForOwner(string owner, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the topics of a GitHub repository.
    /// </summary>
    /// <param name="owner">The username or organization name of the repository owner.</param>
    /// <param name="name">The name of the repository.</param>
    /// <param name="topics">A list of topics to set for the repository.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ReplaceTopics(string owner, string name, List<string> topics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a GitHub repository if it exists.
    /// </summary>
    /// <param name="owner">The username or organization name of the repository owner.</param>
    /// <param name="repository">The name of the repository to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask DeleteIfExists(string owner, string repository, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a GitHub repository exists for the specified owner and name.
    /// </summary>
    /// <param name="owner">The username or organization name of the repository owner.</param>
    /// <param name="name">The name of the repository.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{bool}"/> indicating whether the repository exists.
    /// </returns>
    ValueTask<bool> DoesExist(string owner, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables auto-merge for a GitHub repository.
    /// </summary>
    /// <param name="owner">The username or organization name of the repository owner.</param>
    /// <param name="name">The name of the repository.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask AllowAutoMerge(string owner, string name, CancellationToken cancellationToken = default);
}