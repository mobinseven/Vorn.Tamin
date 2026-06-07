using System.Net;
using System.Text;
using System.Text.Json;
using Vorn.Tamin;

namespace Vorn.Tamin.Tests;

public sealed class AuthClientTests
{
    [Fact]
    public void EnvironmentRoutes_ResolveProductionAndSandboxOperationDifferences()
    {
        var routes = new TaminEnvironmentRoutes();

        var production = routes.Resolve(TaminEndpoint.Production, TaminOperation.GetServices);
        var sandbox = routes.Resolve(TaminEndpoint.Sandbox, TaminOperation.GetServices);

        Assert.Equal("https://soa.tamin.ir/interface/epresc/SendEpresc/v2/services", production.Uri.ToString());
        Assert.Equal("https://ep-test.tamin.ir/api/v2/ws-services", sandbox.Uri.ToString());
    }

    [Fact]
    public void EnvironmentRoutes_UnsupportedEnvironment_FailsExplicitly()
    {
        var routes = new TaminEnvironmentRoutes();

        var ex = Assert.Throws<TaminRouteNotDefinedException>(() => routes.Resolve((TaminEndpoint)99, TaminOperation.Authorize));

        Assert.Equal((TaminEndpoint)99, ex.Environment);
        Assert.Equal(TaminOperation.Authorize, ex.Operation);
    }

    [Fact]
    public void PkceChallenge_Create_GeneratesValidVerifierAndChallenge()
    {
        var pkce = PkceChallenge.Create();

        Assert.InRange(pkce.Verifier.Length, 43, 128);
        Assert.Equal("S256", pkce.Method);
        Assert.DoesNotContain('=', pkce.Challenge);
        Assert.Equal(PkceChallenge.FromVerifier(pkce.Verifier).Challenge, pkce.Challenge);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!")]
    public void PkceChallenge_InvalidVerifier_FailsBeforeTransport(string verifier)
    {
        Assert.ThrowsAny<ArgumentException>(() => PkceChallenge.FromVerifier(verifier));
    }

    [Fact]
    public void AuthClient_CreateAuthorizationUrl_RequiresState()
    {
        var auth = new AuthClient(new HttpClient(new StubHandler()));
        var pkce = PkceChallenge.FromVerifier(new string('a', 43));

        Assert.Throws<ArgumentException>(() => auth.CreateAuthorizationUrl("client", new Uri("https://app/cb"), pkce, ""));
    }

    [Fact]
    public void AuthClient_CreateAuthorizationUrl_IncludesPkceAndStateFields()
    {
        var auth = new AuthClient(new HttpClient(new StubHandler()), TaminEndpoint.Sandbox);
        var pkce = PkceChallenge.FromVerifier(new string('a', 43));

        var uri = auth.CreateAuthorizationUrl("client", new Uri("https://app/callback"), pkce, "state-123");

        Assert.StartsWith("https://ep-test.tamin.ir/auth/server/authorize?", uri.ToString());
        Assert.Contains("response_type=code", uri.Query);
        Assert.Contains("client_id=client", uri.Query);
        Assert.Contains($"code_challenge={pkce.Challenge}", uri.Query);
        Assert.Contains("code_challenge_method=S256", uri.Query);
        Assert.Contains("state=state-123", uri.Query);
    }

    [Fact]
    public async Task AuthClient_ExchangeCodeAsync_PostsFormFields()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var handler = new StubHandler(async (request, _) =>
        {
            captured = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return TokenResponse();
        });
        var auth = new AuthClient(new HttpClient(handler));
        var pkce = PkceChallenge.FromVerifier(new string('b', 43));

        var result = await auth.ExchangeCodeAsync("client", "code", new Uri("https://app/callback"), pkce);

        Assert.Equal("access", result.AccessToken);
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://soa.tamin.ir/auth/server/token", captured.RequestUri!.ToString());
        Assert.Equal("application/x-www-form-urlencoded", captured.Content!.Headers.ContentType!.MediaType);
        Assert.Contains("grant_type=authorization_code", capturedBody);
        Assert.Contains("client_id=client", capturedBody);
        Assert.Contains("code=code", capturedBody);
        Assert.Contains($"code_verifier={pkce.Verifier}", capturedBody);
    }

    [Fact]
    public async Task AuthClient_RefreshTokenV2Async_PostsV2FormFields()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var handler = new StubHandler(async (request, _) =>
        {
            captured = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return TokenResponse();
        });
        var auth = new AuthClient(new HttpClient(handler), TaminEndpoint.Sandbox);

        await auth.RefreshTokenV2Async("client", "refresh", "audience");

        Assert.NotNull(captured);
        Assert.Equal("https://ep-test.tamin.ir/auth/server/v2/token", captured!.RequestUri!.ToString());
        Assert.Equal("application/x-www-form-urlencoded", captured.Content!.Headers.ContentType!.MediaType);
        Assert.Contains("grant_type=refresh_token", capturedBody);
        Assert.Contains("client_id=client", capturedBody);
        Assert.Contains("refresh_token=refresh", capturedBody);
        Assert.Contains("audience=audience", capturedBody);
    }

    [Fact]
    public async Task AuthClient_SignOutAsync_UsesSignOutRoute()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });
        var auth = new AuthClient(new HttpClient(handler), TaminEndpoint.Sandbox);

        await auth.SignOutAsync(new Uri("https://app/signed-out"));

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal("https://ep-test.tamin.ir/auth/signout?redirect_uri=https%3A%2F%2Fapp%2Fsigned-out", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task AuthClient_TransportFailure_PreservesOperationAndEnvironment()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("denied", Encoding.UTF8, "text/plain")
        }));
        var auth = new AuthClient(new HttpClient(handler), TaminEndpoint.Sandbox);
        var pkce = PkceChallenge.FromVerifier(new string('c', 43));

        var ex = await Assert.ThrowsAsync<TaminAuthRequestException>(() =>
            auth.ExchangeCodeAsync("client", "code", new Uri("https://app/callback"), pkce));

        Assert.Equal(TaminEndpoint.Sandbox, ex.Environment);
        Assert.Equal(TaminOperation.TokenExchange, ex.Operation);
        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Equal("denied", ex.Content);
    }


    [Fact]
    public async Task AuthClient_MalformedTokenJson_PreservesOperationAndContent()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>not json</html>", Encoding.UTF8, "text/html")
        }));
        var auth = new AuthClient(new HttpClient(handler), TaminEndpoint.Production);
        var pkce = PkceChallenge.FromVerifier(new string('d', 43));

        var ex = await Assert.ThrowsAsync<TaminAuthRequestException>(() =>
            auth.ExchangeCodeAsync("client", "code", new Uri("https://app/callback"), pkce));

        Assert.Equal(TaminEndpoint.Production, ex.Environment);
        Assert.Equal(TaminOperation.TokenExchange, ex.Operation);
        Assert.Equal(HttpStatusCode.OK, ex.StatusCode);
        Assert.Equal("<html>not json</html>", ex.Content);
        Assert.IsAssignableFrom<JsonException>(ex.InnerException);
    }

    private static HttpResponseMessage TokenResponse() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"access_token\":\"access\",\"refresh_token\":\"refresh\"}", Encoding.UTF8, "application/json")
        };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? handler = null)
        {
            _handler = handler ?? ((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
