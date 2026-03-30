using AwesomeAssertions;
using Xunit;

namespace AgentGit.Tests;

public class GitProcessRunnerTests
{
    private readonly GitProcessRunner _sut = new();

    [Fact]
    public void GetCurrentBranch_returns_current_branch_name()
    {
        // Running in the AgentGit repo itself
        string branch = _sut.GetCurrentBranch(Directory.GetCurrentDirectory());

        branch.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetRemoteUrl_returns_remote_origin_url()
    {
        string url = _sut.GetRemoteUrl(Directory.GetCurrentDirectory());

        url.Should().Contain("github.com");
    }
}
