using Soenneker.GitHub.Repositories.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.GitHub.Repositories.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class GitHubRepositoriesUtilTests : HostedUnitTest
{
    private readonly IGitHubRepositoriesUtil _util;

    public GitHubRepositoriesUtilTests(Host host) : base(host)
    {
        _util = Resolve<IGitHubRepositoriesUtil>(true);
    }

    [Test]
    public void Default()
    {
    }
}
