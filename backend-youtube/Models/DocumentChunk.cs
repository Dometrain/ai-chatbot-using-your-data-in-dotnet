namespace ChatBot.Models;

public record DocumentChunk(
    string Id,
    string Title,
    string Section,
    int ChunkIndex,
    string Content,
    string SourcePageUrl
);
