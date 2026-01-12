using ChatBot.Models;
using Microsoft.Data.Sqlite;

namespace ChatBot.Services;

/// <summary>
/// Stores document chunks (used for all subsequent iterations on the course)
/// </summary>
public class DocumentChunkStore
{
    private const string DbFile = "DocumentChunkStore.db";

    static DocumentChunkStore()
    {
        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
    CREATE TABLE IF NOT EXISTS Chunks(
      Id TEXT PRIMARY KEY,
      Title TEXT,
      Section TEXT,
      ChunkIndex INTEGER,
      Content TEXT,
      SourcePageUrl TEXT
    );
;";
        cmd.ExecuteNonQuery();
    }


    public List<DocumentChunk> GetDocumentChunks(IEnumerable<string> ids)
    {
        var idList = ids?.Distinct().ToList() ?? [];
        if (idList.Count == 0) return [];

        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();

        var paramNames = new List<string>(idList.Count);
        for (int i = 0; i < idList.Count; i++)
        {
            var p = "$p" + i;
            paramNames.Add(p);
            cmd.Parameters.AddWithValue(p, idList[i]);
        }

        var orderByCase =
            "CASE Id " +
            string.Join(" ", idList.Select((id, i) => $"WHEN $p{i} THEN {i}")) +
            " END";

        cmd.CommandText = $@"
SELECT Id, Title, Section, ChunkIndex, Content, SourcePageUrl
FROM Chunks
WHERE Id IN ({string.Join(", ", paramNames)})
ORDER BY {orderByCase};";

        var results = new List<DocumentChunk>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new DocumentChunk(
                Id: reader.GetString(0),
                Title: reader.GetString(1),
                Section: reader.GetString(2),
                ChunkIndex: reader.GetInt32(3),
                Content: reader.GetString(4),
                SourcePageUrl: reader.GetString(5)
            ));
        }

        return results;
    }

    public void SaveDocumentChunk(DocumentChunk chunk)
    {
        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT OR REPLACE INTO Chunks
(Id, Title, Section, ChunkIndex, Content, SourcePageUrl)
VALUES ($id, $title, $section, $chunkIndex, $content, $sourcePageUrl);";
        cmd.Parameters.AddWithValue("$id", chunk.Id);
        cmd.Parameters.AddWithValue("$title", chunk.Title);
        cmd.Parameters.AddWithValue("$section", chunk.Section);
        cmd.Parameters.AddWithValue("$chunkIndex", chunk.ChunkIndex);
        cmd.Parameters.AddWithValue("$content", chunk.Content);
        cmd.Parameters.AddWithValue("$sourcePageUrl", chunk.SourcePageUrl);
        cmd.ExecuteNonQuery();
    }

}