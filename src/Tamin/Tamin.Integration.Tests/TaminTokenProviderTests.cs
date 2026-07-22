using Tamin.Integration.Auth;

namespace Tamin.Integration.Tests;

public sealed class TaminTokenProviderTests
{
    [Fact]
    public void Creates_only_valid_pkce_verifiers()
    {
        for (var index = 0; index < 1_000; index++)
        {
            var verifier = TaminTokenProvider.CreateCodeVerifier(index % 86 + 43);
            Assert.Matches("^[A-Za-z0-9._~-]{43,128}$", verifier);
            Assert.DoesNotContain(':', verifier);
            Assert.DoesNotContain('\\', verifier);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => TaminTokenProvider.CreateCodeVerifier(42));
    }

    [Fact]
    public async Task Rejects_invalid_document_verifier_characters_before_exchange()
    {
        var exchange = new RecordingExchange();
        var provider = new TaminTokenProvider(exchange, "client", "audience", ["soa.tamin.ir"]);
        await Assert.ThrowsAsync<ArgumentException>(() => provider.CompleteAuthorizationAsync("code", "https://callback.example", new string('a', 42) + ":"));
        Assert.Equal(0, exchange.AuthorizationCodeCalls);
    }

    [Fact]
    public async Task Discriminates_authorization_code_and_refresh_grants()
    {
        var exchange = new RecordingExchange();
        var provider = new TaminTokenProvider(exchange, "client", "audience", ["soa.tamin.ir"]);
        await provider.CompleteAuthorizationAsync("code", "https://callback.example", new string('a', 43));
        var token = await provider.GetAuthorizationTokenAsync(new Uri("https://soa.tamin.ir/resource"));
        Assert.Equal("refreshed", token); Assert.Equal(1, exchange.AuthorizationCodeCalls); Assert.Equal(1, exchange.RefreshCalls);
    }

    [Fact]
    public async Task Sends_the_documented_discriminator_for_each_flow_to_the_shared_token_operation()
    {
        var handler = new RecordingHttpHandler();
        var exchange = new TaminDoctorTokenExchange(
            new HttpClient(handler),
            new Uri("https://account.tamin.ir/auth/server/v2/token"),
            isPilot: false);

        await exchange.ExchangeAuthorizationCodeAsync(
            new DoctorAuthorizationCode("code", "client", "audience", "https://callback.example", new string('a', 43)),
            CancellationToken.None);
        await exchange.RefreshAsync("refresh", "client", "audience", CancellationToken.None);

        Assert.Equal(2, handler.FormBodies.Count);
        Assert.Contains("grant_type=authorization_code", handler.FormBodies[0]);
        Assert.Contains("code=code", handler.FormBodies[0]);
        Assert.Contains("code_verifier=", handler.FormBodies[0]);
        Assert.DoesNotContain("refresh_token=", handler.FormBodies[0]);
        Assert.Contains("grant_type=refresh_token", handler.FormBodies[1]);
        Assert.Contains("refresh_token=refresh", handler.FormBodies[1]);
        Assert.DoesNotContain("code=", handler.FormBodies[1]);
        Assert.DoesNotContain("code_verifier=", handler.FormBodies[1]);
    }

    [Fact]
    public async Task Refreshes_before_expiry_and_serializes_concurrent_refreshes()
    {
        var exchange = new RecordingExchange
        {
            AuthorizationToken = new("initial", "refresh", DateTimeOffset.UtcNow.AddSeconds(30)),
            RefreshToken = new("refreshed", "refresh-2", DateTimeOffset.UtcNow.AddHours(1)),
            RefreshDelay = TimeSpan.FromMilliseconds(50)
        };
        var provider = Provider(exchange);
        await provider.CompleteAuthorizationAsync("code", "https://callback.example", new string('a', 43));

        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => provider.GetAuthorizationTokenAsync(new Uri("https://soa.tamin.ir/resource"))));

        Assert.All(results, value => Assert.Equal("refreshed", value));
        Assert.Equal(1, exchange.RefreshCalls);
    }

    [Fact]
    public async Task Missing_expiry_is_not_treated_as_proof_that_a_token_is_fresh()
    {
        var exchange = new RecordingExchange { AuthorizationToken = new("initial", "refresh", null) };
        var provider = Provider(exchange);
        await provider.CompleteAuthorizationAsync("code", "https://callback.example", new string('a', 43));
        Assert.Equal("refreshed", await provider.GetAuthorizationTokenAsync(new Uri("https://soa.tamin.ir/resource")));
        Assert.Equal(1, exchange.RefreshCalls);
    }

    [Fact]
    public async Task Malformed_refresh_clears_cache_preserves_cause_and_never_returns_stale_token()
    {
        var cause = new InvalidOperationException("malformed payload");
        var exchange = new RecordingExchange { RefreshFailure = cause };
        var provider = Provider(exchange);
        await provider.CompleteAuthorizationAsync("code", "https://callback.example", new string('a', 43));

        var failure = await Assert.ThrowsAsync<TaminReauthorizationRequiredException>(() => provider.GetAuthorizationTokenAsync(new Uri("https://soa.tamin.ir/resource")));
        Assert.Same(cause, failure.InnerException);
        var second = await Assert.ThrowsAsync<TaminReauthorizationRequiredException>(() => provider.GetAuthorizationTokenAsync(new Uri("https://soa.tamin.ir/resource")));
        Assert.Contains("no usable refresh token", second.InnerException!.Message);
        Assert.Equal(1, exchange.RefreshCalls);
    }

    [Fact]
    public async Task Empty_refresh_token_requires_reauthorization()
    {
        var exchange = new RecordingExchange { AuthorizationToken = new("initial", "", DateTimeOffset.UtcNow.AddSeconds(1)) };
        var provider = Provider(exchange);
        await provider.CompleteAuthorizationAsync("code", "https://callback.example", new string('a', 43));
        var failure = await Assert.ThrowsAsync<TaminReauthorizationRequiredException>(() => provider.GetAuthorizationTokenAsync(new Uri("https://soa.tamin.ir/resource")));
        Assert.NotNull(failure.InnerException);
        Assert.Equal(0, exchange.RefreshCalls);
    }

    [Fact]
    public async Task Empty_access_token_from_refresh_is_a_typed_reauthorization_failure()
    {
        var exchange = new RecordingExchange { RefreshToken = new(" ", "refresh", DateTimeOffset.UtcNow.AddHours(1)) };
        var provider = Provider(exchange);
        await provider.CompleteAuthorizationAsync("code", "https://callback.example", new string('a', 43));
        var failure = await Assert.ThrowsAsync<TaminReauthorizationRequiredException>(() => provider.GetAuthorizationTokenAsync(new Uri("https://soa.tamin.ir/resource")));
        Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    private static TaminTokenProvider Provider(IDoctorTokenExchange exchange) => new(exchange, "client", "audience", ["soa.tamin.ir"]);

    private sealed class RecordingExchange : IDoctorTokenExchange
    {
        public int AuthorizationCodeCalls { get; private set; }
        public int RefreshCalls { get; private set; }
        public DoctorToken AuthorizationToken { get; init; } = new("initial", "refresh", DateTimeOffset.UtcNow.AddMinutes(-1));
        public DoctorToken RefreshToken { get; init; } = new("refreshed", "refresh", DateTimeOffset.UtcNow.AddHours(1));
        public Exception? RefreshFailure { get; init; }
        public TimeSpan RefreshDelay { get; init; }
        public Task<DoctorToken> ExchangeAuthorizationCodeAsync(DoctorAuthorizationCode grant, CancellationToken cancellationToken)
        {
            AuthorizationCodeCalls++;
            return Task.FromResult(AuthorizationToken);
        }
        public async Task<DoctorToken> RefreshAsync(string refreshToken, string clientId, string audience, CancellationToken cancellationToken)
        {
            RefreshCalls++;
            if (RefreshDelay > TimeSpan.Zero) await Task.Delay(RefreshDelay, cancellationToken);
            if (RefreshFailure is not null) throw RefreshFailure;
            return RefreshToken;
        }
    }

    private sealed class RecordingHttpHandler : HttpMessageHandler
    {
        public List<string> FormBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            FormBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"token\",\"refresh_token\":\"refresh\"}")
            };
        }
    }
}
