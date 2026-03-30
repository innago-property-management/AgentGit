using AwesomeAssertions;
using Xunit;

namespace AgentGit.Tests;

public class RemoteUrlParserTests
{
    [Theory]
    [InlineData("https://github.com/innago-property-management/AgentGit.git", "innago-property-management", "AgentGit")]
    [InlineData("https://github.com/stand-sure/stand-sure-ai.git", "stand-sure", "stand-sure-ai")]
    [InlineData("https://github.com/owner/repo", "owner", "repo")]
    public void Parse_https_url_returns_owner_and_repo(string url, string expectedOwner, string expectedRepo)
    {
        var (owner, repo) = RemoteUrlParser.Parse(url);

        owner.Should().Be(expectedOwner);
        repo.Should().Be(expectedRepo);
    }

    [Theory]
    [InlineData("git@github.com:innago-property-management/AgentGit.git", "innago-property-management", "AgentGit")]
    [InlineData("git@github.com:stand-sure/stand-sure-ai.git", "stand-sure", "stand-sure-ai")]
    public void Parse_ssh_url_returns_owner_and_repo(string url, string expectedOwner, string expectedRepo)
    {
        var (owner, repo) = RemoteUrlParser.Parse(url);

        owner.Should().Be(expectedOwner);
        repo.Should().Be(expectedRepo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("https://gitlab.com/owner/repo.git")]
    public void Parse_invalid_url_returns_empty_strings(string url)
    {
        var (owner, repo) = RemoteUrlParser.Parse(url);

        owner.Should().BeEmpty();
        repo.Should().BeEmpty();
    }

    [Fact]
    public void Parse_url_with_only_owner_returns_empty_strings()
    {
        var (owner, repo) = RemoteUrlParser.Parse("https://github.com/owner");

        owner.Should().BeEmpty();
        repo.Should().BeEmpty();
    }
}
