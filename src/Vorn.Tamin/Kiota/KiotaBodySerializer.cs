using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;

namespace Vorn.Tamin.Kiota;

/// <summary>Serializes Kiota parsable bodies for friendly gateway request information.</summary>
internal sealed class KiotaBodySerializer
{
    private readonly JsonSerializationWriterFactory _writerFactory = new();

    public Stream Serialize<T>(T body) where T : IParsable
    {
        if (body is null)
            throw new ArgumentNullException(nameof(body));

        var writer = _writerFactory.GetSerializationWriter("application/json");
        writer.WriteObjectValue(null, body);
        return writer.GetSerializedContent();
    }

    public Stream SerializeCollection<T>(IEnumerable<T> body) where T : IParsable
    {
        if (body is null)
            throw new ArgumentNullException(nameof(body));

        var writer = _writerFactory.GetSerializationWriter("application/json");
        writer.WriteCollectionOfObjectValues(null, body);
        return writer.GetSerializedContent();
    }
}
