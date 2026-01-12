using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.Text;
using ChatBot.Models;

namespace ChatBot.Services;

public class TranscriptSplitter
{
    // 1) Your sliding windows function (from earlier)
    static IEnumerable<(int startWord, int endWord)> SlidingWindows(
        int totalWords, int windowWords = 500, int overlapWords = 75)
    {
        if (totalWords <= 0) yield break;
        int step = Math.Max(1, windowWords - overlapWords);
        for (int start = 0; start < totalWords; start += step)
        {
            int end = Math.Min(start + windowWords, totalWords); // end is EXCLUSIVE
            yield return (start, end);
            if (end == totalWords) yield break;
        }
    }

    // 3) Helper to split transcript into words (robust to messy whitespace)
    static string[] SplitWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        return Regex.Split(text.Trim(), @"\s+")
                    .Where(w => w.Length > 0)
                    .ToArray();
    }

    // 4) Build chunks using sliding windows
    public List<DocumentChunk> Chunk(
        string title,
        string transcriptText,
        int windowWords = 500,
        int overlapWords = 75)
    {
        var words = SplitWords(transcriptText);
        var chunks = new List<DocumentChunk>(capacity: Math.Max(1, (words.Length + windowWords - 1) / windowWords));
        int i = 0;

        foreach (var (start, end) in SlidingWindows(words.Length, windowWords, overlapWords))
        {
            var sliceLen = end - start;
            // Avoid excessive allocations by copying once
            var slice = new string[sliceLen];
            Array.Copy(words, start, slice, 0, sliceLen);
            var text = string.Join(' ', slice);

            var id = $"{title} (section {i:D2})"; // stable, sortable
            chunks.Add(new DocumentChunk(id, title, $"Section {i + 1}", i + 1, text, "https://www.youtube.com/@traintocode"));
            i++;
        }

        return chunks;
    }

}