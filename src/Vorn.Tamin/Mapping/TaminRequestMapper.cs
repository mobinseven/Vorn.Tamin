using System.Text.Json;
using Vorn.Tamin.Kiota.Models;

namespace Vorn.Tamin.Mapping;

/// <summary>Maps friendly request DTOs into generated Kiota request models.</summary>
internal static class TaminRequestMapper
{
    public static SendEprescRequest ToSendEprescRequest<TPayload>(TPayload payload)
    {
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));

        if (payload is SendEprescRequest generatedRequest)
            return generatedRequest;

        var request = new SendEprescRequest();
        var json = JsonSerializer.SerializeToElement(payload);
        if (json.ValueKind != JsonValueKind.Object)
        {
            request.AdditionalData["value"] = ToAdditionalDataValue(json);
            return request;
        }

        foreach (var property in json.EnumerateObject())
        {
            request.AdditionalData[property.Name] = ToAdditionalDataValue(property.Value);
        }

        return request;
    }

    private static object? ToAdditionalDataValue(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ToAdditionalDataValue(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(ToAdditionalDataValue).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when element.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
}
