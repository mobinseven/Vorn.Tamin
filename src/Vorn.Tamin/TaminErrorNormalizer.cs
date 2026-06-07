using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Vorn.Tamin;

/// <summary>Converts EP.Tamin provider failures into normalized SDK error metadata.</summary>
public sealed class TaminErrorNormalizer
{
    /// <summary>Shared stateless instance of the normalizer.</summary>
    public static readonly TaminErrorNormalizer Shared = new();

    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

    private static readonly string[] MessagePropertyNames =
    [
        "message", "error", "reason", "reasonText", "data", "detail", "title"
    ];

    private static readonly TaminErrorRule TemporaryProviderServiceRule = new("temporary-provider-service", TaminErrorCategory.Retryable, _ => true);

    private static readonly IReadOnlyList<TaminErrorRule> Rules =
    [
        new("invalid-prescription-service-pair", TaminErrorCategory.ClientPreventable, ContainsAll("presctype", "srvtype")),
        new("missing-laboratory-subgroup", TaminErrorCategory.ClientPreventable, ContainsAny("laboratory subgroup", "lab subgroup", "servicegroup", "srvgroup", "pargrp")),
        new("invalid-quantity", TaminErrorCategory.ClientPreventable, ContainsAny("negative quantity", "null quantity", "quantity", "srvqty", "drugqty")),
        new("doctor-enrollment-or-activation", TaminErrorCategory.SupportRequired, ContainsAny("doctor enrollment", "doctor activation", "not active", "inactive doctor", "not enrolled", "enrollment")),
        new("doctor-national-code-mobile-mismatch", TaminErrorCategory.SupportRequired, ContainsAll("doctor", "mobile", "national")),
        new("empty-payload", TaminErrorCategory.ClientPreventable, ContainsAny("empty payload", "empty body", "request body is empty", "payload is empty")),
        new("missing-or-malformed-patient-mobile", TaminErrorCategory.ClientPreventable, ContainsAll("patient", "mobile")),
        new("invalid-patient-national-code", TaminErrorCategory.ClientPreventable, ContainsAll("patient", "national")),
        new("unknown-service-code", TaminErrorCategory.ClientPreventable, ContainsAny("unknown srvcode", "invalid srvcode", "srvcode", "service code")),
        new("missing-or-invalid-prescription-type", TaminErrorCategory.ClientPreventable, ContainsAny("prescription type", "presctype")),
        new("duplicate-submission", TaminErrorCategory.Retryable, ContainsAny("duplicate", "already exists", "already registered", "repeated request")),
        new("date-format", TaminErrorCategory.ClientPreventable, ContainsAny("date format", "invalid date", "prescdate", "visit date")),
        new("future-date", TaminErrorCategory.ClientPreventable, ContainsAny("future date", "date is future", "future")),
        new("invalid-drug-amount-or-instruction", TaminErrorCategory.ClientPreventable, ContainsAny("drugamntid", "druginstid", "drug amount", "drug instruction")),
        new("provider-contract-mismatch", TaminErrorCategory.ProviderContractMismatch, ContainsAny("id_client", "client_id", "isdentalservice", "string or number", "number or string"))
    ];

    /// <summary>Normalizes a provider failure body and preserves operation/environment context.</summary>
    public TaminProviderError Normalize(
        string? operationName,
        TaminEndpoint? environment,
        HttpStatusCode? statusCode,
        string? reasonPhrase,
        string? providerBody)
    {
        var providerMessage = ExtractProviderMessage(providerBody) ?? reasonPhrase;
        var searchable = NormalizeText(string.Join(' ', providerMessage, reasonPhrase));
        var rule = Rules.FirstOrDefault(candidate =>
            candidate.IsMatch(searchable) ||
            (!string.IsNullOrWhiteSpace(providerBody) && candidate.IsMatch(providerBody)));

        if (rule is null && statusCode is >= HttpStatusCode.InternalServerError)
            rule = TemporaryProviderServiceRule;

        var category = rule?.Category ?? TaminErrorCategory.UnknownProviderError;
        var code = rule?.Code ?? "unknown-provider-error";

        return new TaminProviderError(
            category,
            code,
            operationName,
            environment,
            statusCode,
            reasonPhrase,
            providerMessage,
            providerBody);
    }

    private static string? ExtractProviderMessage(string? providerBody)
    {
        if (string.IsNullOrWhiteSpace(providerBody))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(providerBody);
            return FindMessage(doc.RootElement);
        }
        catch (JsonException)
        {
            return providerBody;
        }
    }

    private static string? FindMessage(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return element.ValueKind == JsonValueKind.String ? element.GetString() : null;

        foreach (var propertyName in MessagePropertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                continue;

            if (property.ValueKind == JsonValueKind.String)
                return property.GetString();

            var nested = FindMessage(property);
            if (!string.IsNullOrWhiteSpace(nested))
                return nested;
        }

        return null;
    }

    private static string NormalizeText(string value)
        => WhitespaceRegex.Replace(value, " ").Trim();

    private static Func<string, bool> ContainsAny(params string[] terms)
        => text => terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static Func<string, bool> ContainsAll(params string[] terms)
        => text => terms.All(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));

    private sealed record TaminErrorRule(string Code, TaminErrorCategory Category, Func<string, bool> IsMatch);
}
