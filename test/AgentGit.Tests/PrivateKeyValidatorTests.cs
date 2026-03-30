using AwesomeAssertions;
using Xunit;

namespace AgentGit.Tests;

public class PrivateKeyValidatorTests : IDisposable
{
    private readonly string _validKeyPath;

    public PrivateKeyValidatorTests()
    {
        _validKeyPath = Path.GetTempFileName();
        File.WriteAllText(_validKeyPath, "fake-key-content");

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_validKeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    [Fact]
    public void Validate_returns_success_for_valid_key()
    {
        var result = PrivateKeyValidator.Validate(_validKeyPath);

        result.IsValid.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Validate_returns_error_when_file_not_found()
    {
        var result = PrivateKeyValidator.Validate("/nonexistent/path/key.pem");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    [Trait("Category", "Unix")]
    public void Validate_returns_error_when_world_readable()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix-only test");

#pragma warning disable CA1416 // Platform compatibility - guarded by SkipWhen above
        File.SetUnixFileMode(_validKeyPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherRead);
#pragma warning restore CA1416

        var result = PrivateKeyValidator.Validate(_validKeyPath);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("permissive");
    }

    public void Dispose()
    {
        File.Delete(_validKeyPath);
    }
}
