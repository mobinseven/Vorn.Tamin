using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Tamin.Integration.Mapping;

namespace Tamin.Integration.Http;

/// <summary>Carries the contract operationId to the response handler, so failures are reported without the route's personal data.</summary>
internal sealed record TaminOperationOption(string OperationId) : IRequestOption
{
    public const string Unknown = "unknown";

    public static string Resolve(HttpRequestMessage request) =>
        request.Options.TryGetValue(new HttpRequestOptionsKey<IRequestOption>(typeof(TaminOperationOption).FullName!), out var option) && option is TaminOperationOption operation
            ? operation.OperationId
            : Unknown;
}

internal static class TaminOperationSender
{
    public static async Task<TResult> SendAsync<TModel, TResult>(IRequestAdapter adapter, string operationId, RequestInformation request, ParsableFactory<TModel> factory, Func<TModel, TResult> map, CancellationToken cancellationToken)
        where TModel : IParsable
    {
        request.AddRequestOptions([new TaminOperationOption(operationId)]);
        try
        {
            var model = await adapter.SendAsync(request, factory, cancellationToken: cancellationToken).ConfigureAwait(false);
            return model is null ? throw new TaminApiException(null, operationId, string.Empty) : map(model);
        }
        catch (JsonException exception)
        {
            throw new TaminApiException(null, operationId, string.Empty, exception);
        }
    }

    /// <summary>
    /// Reads the raw body. The contract defines no schema for these operations, so the generated JsonObject
    /// model cannot be trusted to keep it: it reads only object roots and wraps values in Kiota node types.
    /// An empty body yields a payload whose ValueKind is Undefined.
    /// </summary>
    public static async Task<UnconstrainedResponse> SendOpaqueAsync(IRequestAdapter adapter, string operationId, RequestInformation request, CancellationToken cancellationToken)
    {
        request.AddRequestOptions([new TaminOperationOption(operationId)]);
        await using var body = await adapter.SendPrimitiveAsync<Stream>(request, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (body is null) return new(default);
        using var buffer = new MemoryStream();
        await body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (buffer.Length == 0) return new(default);
        try
        {
            using var document = JsonDocument.Parse(buffer.ToArray());
            return new(document.RootElement.Clone());
        }
        catch (JsonException exception)
        {
            throw new TaminApiException(null, operationId, System.Text.Encoding.UTF8.GetString(buffer.ToArray()), exception);
        }
    }

    /// <summary>A route value: a blank one would silently address a different route.</summary>
    public static string Route(string value, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        return value;
    }
}
