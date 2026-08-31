[![](https://img.shields.io/nuget/v/soenneker.github.repositories.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.github.repositories/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.github.repositories/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.github.repositories/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.github.repositories.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.github.repositories/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.github.repositories/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.github.repositories/actions/workflows/codeql.yml)

# Soenneker.GitHub.Repositories

Creates, discovers, configures, and deletes GitHub repositories through the REST and GraphQL APIs.

## Installation

```bash
dotnet add package Soenneker.GitHub.Repositories
```

## Configure and register

```json
{
  "GH": {
    "Token": "your-github-token"
  }
}
```

```csharp
using Soenneker.GitHub.Repositories.Registrars;

services.AddGitHubRepositoriesUtilAsSingleton();
```

The token's repository and organization permissions determine which operations succeed. Creation, settings changes, sponsorship changes, and deletion require write or administration permissions appropriate to the target.

## Discover repositories

```csharp
FullRepository? repository = await repositories.GetByName(
    "example-org", "example-repository", cancellationToken);

List<MinimalRepository> recent = await repositories.GetAllForOwner(
    "example-org",
    startAt: DateTimeOffset.UtcNow.AddMonths(-1),
    cancellationToken: cancellationToken);
```

`GetByName()` returns `null` only for GitHub's 404 response. Authentication, permission, rate-limit, and transport failures propagate. `GetAllForOwnerIncrementally()` yields pages as they arrive and applies the optional creation-date window.

## Create and configure

```csharp
FullRepository created = await repositories.CreateForOrg(
    org: "example-org",
    name: "example-repository",
    description: "Example service",
    isPrivate: true,
    allowSquashMerge: true,
    cancellationToken: cancellationToken);

await repositories.ReplaceTopics(
    "example-org", "example-repository",
    ["dotnet", "service"],
    cancellationToken);
```

`ReplaceTopics()` replaces the entire topic collection; passing an empty list clears it. `CreateUnique()` probes the base name and numeric suffixes, then creates the first name reported as absent. A concurrent creator can still win that race and cause GitHub to reject the request.

## Mutating operations

`ToggleAutoMergeOnAllRepos()` attempts every repository and throws an `AggregateException` afterward if any updates failed, so partial completion is visible. `ToggleSponsorships()` uses GitHub's GraphQL mutation over the same authenticated HTTP transport.

`DeleteIfExists()` permanently deletes the repository when GitHub confirms it exists. It does not treat access failures as absence; verify the owner and repository before calling it.
