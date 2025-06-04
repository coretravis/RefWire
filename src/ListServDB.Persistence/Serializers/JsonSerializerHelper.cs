using System.Text.Json;

namespace ListServDB.Persistence.Serializers;

/// <summary>
/// Helper class for serializing and deserializing objects using JSON.
/// </summary>
public static class JsonSerializerHelper
{
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    /// <summary>
    /// Serializes the given object to a JSON string.
    /// </summary>
    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    /// <summary>
    /// Deserializes the given JSON string into an object of type T.
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options);
    }
}
