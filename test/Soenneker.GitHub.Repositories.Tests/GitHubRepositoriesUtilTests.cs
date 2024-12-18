using Soenneker.GitHub.Repositories.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;


namespace Soenneker.GitHub.Repositories.Tests;

[Collection("Collection")]
public class GitHubRepositoriesUtilTests : FixturedUnitTest
{
    private readonly IGitHubRepositoriesUtil _util;

    public GitHubRepositoriesUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IGitHubRepositoriesUtil>(true);
    }
}
