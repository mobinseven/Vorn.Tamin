using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tamin.Integration.Http;

namespace Tamin.Integration.Tests;

public sealed class TaminHttpPipelineTests
{
    [Theory]
    [InlineData("account.tamin.ir", "beginAuthorization", HttpStatusCode.BadRequest)]
    [InlineData("account.tamin.ir", "exchangeOrRefreshDoctorToken", HttpStatusCode.InternalServerError)]
    [InlineData("soa.tamin.ir", "getPatientEntitlement", HttpStatusCode.UnprocessableEntity)]
    [InlineData("soa.tamin.ir", "getReferralCartable", HttpStatusCode.ServiceUnavailable)]
    [InlineData("api.tamin.ir", "listPatientDiseases", HttpStatusCode.NotFound)]
    [InlineData("api.tamin.ir", "calculateFamilyDoctorPatientShare", HttpStatusCode.BadGateway)]
    public async Task Captures_4xx_and_5xx_per_host_with_operation_keyed_logging(
        string host,
        string operationId,
        HttpStatusCode statusCode)
    {
        const string body = "{\"error\":\"provider detail\"}";
        var logger = new RecordingLogger<TaminResponseHandler>();
        using var handler = new TaminResponseHandler(logger, _ => operationId)
        {
            InnerHandler = new StubHandler(_ => new HttpResponseMessage(statusCode) { Content = new StringContent(body) })
        };
        using var client = new HttpClient(handler);

        var exception = await Assert.ThrowsAsync<TaminApiException>(() => client.GetAsync($"https://{host}/test"));

        Assert.Equal((int)statusCode, exception.StatusCode);
        Assert.Equal(operationId, exception.OperationId);
        Assert.Equal(body, exception.RawBody);
        var entry = Assert.Single(logger.Entries);
        Assert.Contains(operationId, entry);
        Assert.Contains(body, entry);
    }

    [Fact]
    public async Task Preserves_non_success_status_operation_and_raw_body()
    {
        const string body = "{\"error\":\"provider detail\"}";
        using var handler = new TaminResponseHandler(NullLogger<TaminResponseHandler>.Instance, _ => "getPatientEntitlement")
        {
            InnerHandler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) })
        };
        using var client = new HttpClient(handler);
        var exception = await Assert.ThrowsAsync<TaminApiException>(() => client.GetAsync("https://soa.tamin.ir/test"));
        Assert.Equal(400, exception.StatusCode); Assert.Equal("getPatientEntitlement", exception.OperationId); Assert.Equal(body, exception.RawBody);
    }

    [Fact]
    public async Task Retries_transient_get_but_not_mutating_post()
    {
        var getCalls = 0;
        using var getHandler = new TaminTransientFaultHandler { InnerHandler = new StubHandler(_ => ++getCalls < 3 ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : new HttpResponseMessage(HttpStatusCode.OK)) };
        using var getClient = new HttpClient(getHandler);
        Assert.Equal(HttpStatusCode.OK, (await getClient.GetAsync("https://soa.tamin.ir/test")).StatusCode); Assert.Equal(3, getCalls);

        var postCalls = 0;
        using var postHandler = new TaminTransientFaultHandler { InnerHandler = new StubHandler(_ => { postCalls++; return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable); }) };
        using var postClient = new HttpClient(postHandler);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await postClient.PostAsync("https://soa.tamin.ir/test", null)).StatusCode); Assert.Equal(1, postCalls);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request));
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add(formatter(state, exception));
    }
}
