// Program.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public record VideoDoc(
    string ChannelName,
    string VideoTitle,
    string Transcript
);

public static class JsonlReader
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Synchronous iterator that streams a .jsonl file.
    /// Yields each successfully parsed object; logs parse errors with line numbers.
    /// </summary>
    public static IEnumerable<VideoDoc> ReadJsonl(string path)
    {
        using var sr = new StreamReader(path);
        string? line;
        int lineNumber = 0;

        while ((line = sr.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var obj = JsonSerializer.Deserialize<VideoDoc>(line, _jsonOptions);
            if (obj is not null) yield return obj;
            else Console.Error.WriteLine($"[jsonl] Line {lineNumber}: deserialized to null.");
        }
    }

    /// <summary>
    /// Async variant for async pipelines.
    /// </summary>
    public static async IAsyncEnumerable<VideoDoc> ReadJsonlAsync(string path)
    {
        using var sr = new StreamReader(path);
        int lineNumber = 0;

        while (await sr.ReadLineAsync() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            VideoDoc? obj = null;
            try
            {
                obj = JsonSerializer.Deserialize<VideoDoc>(line, _jsonOptions);
            }
            catch (JsonException jx)
            {
                Console.Error.WriteLine($"[jsonl] Line {lineNumber}: JSON parse error: {jx.Message}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[jsonl] Line {lineNumber}: {ex.GetType().Name}: {ex.Message}");
            }

            if (obj is not null) yield return obj;
        }
    }
}
