using Pinecone;
using Microsoft.Extensions.AI;
using System.Collections.Immutable;

namespace ChatBot.Services;

public class IndexBuilder(
    StringEmbeddingGenerator embeddingGenerator,
    IndexClient pineconeIndex,
    DocumentChunkStore chunkStore,
    TranscriptSplitter splitter)
{
    public async Task BuildIndex()
    {
        var jsonlPath = Path.Combine(AppContext.BaseDirectory, "transcripts.jsonl");
        var items = JsonlReader.ReadJsonl(jsonlPath);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Transcript)) continue;
            
            var chunks = splitter.Chunk(item.VideoTitle, item.Transcript);

            var stringsToEmbed = chunks.Select(c => $"{c.Title} (part {c.ChunkIndex})\n\n{c.Content}");

            var embeddings = await embeddingGenerator.GenerateAsync(
                stringsToEmbed,
                new EmbeddingGenerationOptions { Dimensions = 512 }
            );

            var vectors = chunks.Select((chunk, index) => new Vector
            {
                Id = chunk.Id,
                Values = embeddings[index].Vector.ToArray(),
                Metadata = new Metadata
                {
                    { "title", chunk.Title },
                    { "chunk_index", chunk.ChunkIndex }
                }
            });

            await pineconeIndex.UpsertAsync(new UpsertRequest
            {
                Vectors = vectors
            });

            foreach (var chunk in chunks)
            {
                chunkStore.SaveDocumentChunk(chunk);
            }
        }
    }
}
