using System.Net;
using Tamin.Integration.Auth;

namespace Tamin.Integration.Tests;

/// <summary>Proves the token store and its failures behave through the public facade, not only inside the provider.</summary>
public sealed class TaminTokenStoreFacadeTests
{
    [Fact]
    public async Task An_empty_store_surfaces_reauthorization_unwrapped_and_sends_nothing()
    {
        var handler = new CountingHandler();
        var client = TaminClient.CreateProduction(new("client", "audience", handler, null, new Store(null)));

        await Assert.ThrowsAsync<TaminReauthorizationRequiredException>(() => client.ListServicesAsync("01"));
        Assert.Equal(0, handler.Requests);
    }

    [Fact]
    public async Task A_stored_fresh_token_is_sent_as_the_bearer()
    {
        var handler = new CountingHandler();
        var client = TaminClient.CreatePilot(new("portal-js", "audience", handler, null, new Store(new("stored-token", "refresh", DateTimeOffset.UtcNow.AddHours(1)))));

        await client.ListServicesAsync("01");

        Assert.Equal(1, handler.Requests);
        Assert.Equal("Bearer stored-token", handler.Authorization);
    }

    private sealed class Store(DoctorToken? token) : ITaminTokenStore
    {
        public Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken) => Task.FromResult<IAsyncDisposable>(new Lease());
        public Task<DoctorToken?> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(token);
        public Task WriteAsync(DoctorToken? value, CancellationToken cancellationToken) { token = value; return Task.CompletedTask; }
        private sealed class Lease : IAsyncDisposable { public ValueTask DisposeAsync() => ValueTask.CompletedTask; }
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }
        public string? Authorization { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json") });
        }
    }
}
