using System.Text.Json;
using System.Text.Json.Serialization;

namespace IntegrationTests.Helpers
{
    // ─────────────────────────────────────────────────────────────────────────
    // JSONOPTIONS
    //
    // The Hospital API is configured with JsonStringEnumConverter in Program.cs,
    // so all enum values are serialized as strings ("Female", "Scheduled", etc.).
    //
    // When tests deserialize responses with ReadFromJsonAsync<T>(), the default
    // System.Text.Json options try to parse enum strings as integers → exception.
    //
    // We define one shared JsonSerializerOptions here and pass it to every
    // ReadFromJsonAsync<T>() call so deserialization matches the API's output.
    // ─────────────────────────────────────────────────────────────────────────
    public static class TestJsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}
