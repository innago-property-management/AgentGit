using AwesomeAssertions;
using Xunit;

namespace AgentGit.Tests;

public class AskPassScriptManagerTests : IDisposable
{
    private string? _scriptPath;

    [Fact]
    public void Create_returns_path_to_existing_file()
    {
        _scriptPath = AskPassScriptManager.Create();

        File.Exists(_scriptPath).Should().BeTrue();
    }

    [Fact]
    public void Create_produces_sh_script_on_unix()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix-only test");

        _scriptPath = AskPassScriptManager.Create();

        _scriptPath.Should().EndWith(".sh");
        string content = File.ReadAllText(_scriptPath);
        content.Should().Contain("AGENTGIT_TOKEN");
        content.Should().StartWith("#!/bin/sh");
    }

    [Fact]
    public void Create_produces_cmd_script_on_windows()
    {
        Assert.SkipWhen(!OperatingSystem.IsWindows(), "Windows-only test");

        _scriptPath = AskPassScriptManager.Create();

        _scriptPath.Should().EndWith(".cmd");
        string content = File.ReadAllText(_scriptPath);
        content.Should().Contain("AGENTGIT_TOKEN");
    }

    [Fact]
    public void Cleanup_deletes_the_script()
    {
        _scriptPath = AskPassScriptManager.Create();
        File.Exists(_scriptPath).Should().BeTrue();

        AskPassScriptManager.Cleanup(_scriptPath);

        File.Exists(_scriptPath).Should().BeFalse();
        _scriptPath = null; // Already cleaned
    }

    [Fact]
    public void Cleanup_does_not_throw_when_file_missing()
    {
        Action act = () => AskPassScriptManager.Cleanup("/nonexistent/path/script.sh");

        act.Should().NotThrow();
    }

    public void Dispose()
    {
        if (_scriptPath is not null && File.Exists(_scriptPath))
        {
            File.Delete(_scriptPath);
        }
    }
}
