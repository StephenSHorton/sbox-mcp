using System.Text.Json;
using System.Text.Json.Serialization;

namespace SboxMcp.Server.Bridge;

public record BridgeRequest
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("command")]
    public string Command { get; init; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; init; }
}

public record BridgeResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
