using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace AgentGit.Tests;

public class GitHubAppAuthenticatorTests
{
    [Fact]
    public async Task AuthenticateAsync_parses_installation_and_token()
    {
        var installationJson = JsonSerializer.Serialize(
            new InstallationResponse { Id = 12345 },
            GitHubApiJsonContext.Default.InstallationResponse);

        var tokenJson = JsonSerializer.Serialize(
            new AccessTokenResponse { Token = "ghs_test_token", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) },
            GitHubApiJsonContext.Default.AccessTokenResponse);

        var handler = new FakeHttpHandler(
        [
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(installationJson, System.Text.Encoding.UTF8, "application/json") },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(tokenJson, System.Text.Encoding.UTF8, "application/json") },
        ]);

        var httpClient = new HttpClient(handler);
        var sut = new GitHubAppAuthenticator(httpClient);

        var (installationId, token, expiresAt) = await sut.AuthenticateAsync("fake-jwt", "owner", "repo", TestContext.Current.CancellationToken);

        installationId.Should().Be(12345);
        token.Should().Be("ghs_test_token");
        expiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task AuthenticateAsync_throws_on_missing_installation()
    {
        var handler = new FakeHttpHandler(
        [
            new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") },
        ]);

        var httpClient = new HttpClient(handler);
        var sut = new GitHubAppAuthenticator(httpClient);

        Func<Task> act = () => sut.AuthenticateAsync("fake-jwt", "owner", "repo", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private sealed class FakeHttpHandler(List<HttpResponseMessage> responses) : HttpMessageHandler
    {
        private int _callIndex;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_callIndex >= responses.Count)
            {
                throw new InvalidOperationException($"Unexpected HTTP call #{_callIndex}: {request.Method} {request.RequestUri}");
            }

            return Task.FromResult(responses[_callIndex++]);
        }
    }
}
