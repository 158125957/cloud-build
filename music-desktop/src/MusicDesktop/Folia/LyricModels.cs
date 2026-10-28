using System.Text.Json.Serialization;

namespace MusicDesktop.Folia;

public sealed class LyricWord
{
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("startTime")] public double StartTime { get; set; }
    [JsonPropertyName("endTime")] public double EndTime { get; set; }
}

public sealed class LyricLine
{
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("startTime")] public double StartTime { get; set; }
    [JsonPropertyName("endTime")] public double EndTime { get; set; }
    [JsonPropertyName("words")] public List<LyricWord> Words { get; set; } = new();
    [JsonPropertyName("translation")] public string? Translation { get; set; }
    [JsonPropertyName("romanization")] public string? Romanization { get; set; }
}

public sealed class LyricData
{
    [JsonPropertyName("offset")] public int Offset { get; set; }
    [JsonPropertyName("clipStartMs")] public int ClipStartMs { get; set; }
    [JsonPropertyName("lines")] public List<LyricLine> Lines { get; set; } = new();
    [JsonPropertyName("wordByWord")] public bool WordByWord { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("artist")] public string? Artist { get; set; }
}
