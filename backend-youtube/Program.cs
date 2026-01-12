using ChatBot;
using ChatBot.Services;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);
Startup.ConfigureServices(builder);
var app = builder.Build();

app.UseCors("FrontendCors");

// Comment/Uncomment to do indexing when you run the project (you only need to do this once)...
var indexer = app.Services.GetRequiredService<IndexBuilder>();
await indexer.BuildIndex();

// GET /search?query=...
app.MapGet("/search", async (string query, VectorSearchService search) =>
{
    var results = await search.FindTopKChunks(query, 3);
    return Results.Ok(results);
});

// GET /ask?question=...
app.MapGet("/ask", async (string question, RagQuestionService rag) =>
{
    var result = await rag.AnswerQuestion(question);
    return Results.Ok(result);
});

// POST /chat
app.MapPost("/chat", async (
    List<ChatMessage> messages,
    IChatClient client,
    ChatOptions chatOptions,
    PromptService promptService) =>
{
    var withSystemPrompt = (new[] { new ChatMessage(ChatRole.System, promptService.ChatSystemPrompt) })
                            .Concat(messages)
                            .ToList();

    var response = await client.GetResponseAsync(withSystemPrompt, chatOptions);
    return Results.Ok(response.Messages);
});

app.Run();