using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Pinecone;
using ChatBot.Services;

namespace ChatBot;

static class Startup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var openAiKey = builder.RequireEnv("OPENAI_API_KEY");
        var pineconeKey = builder.RequireEnv("PINECONE_API_KEY");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("FrontendCors", policy =>
                policy
                    .WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
            );
        });

        builder.Services.AddSingleton<StringEmbeddingGenerator>(s => new OpenAI.Embeddings.EmbeddingClient(
                model: "text-embedding-3-small",
                apiKey: openAiKey
            ).AsIEmbeddingGenerator());

        builder.Services.AddSingleton<IndexClient>(s => new PineconeClient(pineconeKey).Index("youtube-transcripts"));

        builder.Services.AddSingleton<DocumentChunkStore>(s => new DocumentChunkStore());

        builder.Services.AddSingleton<VectorSearchService>();

        builder.Services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));

        builder.Services.AddSingleton<ILoggerFactory>(sp =>
            LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information)));

        builder.Services.AddSingleton<IChatClient>(sp =>
         {
             var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
             var client = new OpenAI.Chat.ChatClient(
                  "gpt-5-mini",
                  openAiKey).AsIChatClient();

             return new ChatClientBuilder(client)
                 .UseLogging(loggerFactory)
                 .UseFunctionInvocation(loggerFactory, c =>
                 {
                     c.IncludeDetailedErrors = true;
                 })
                 .Build(sp);
         });

        builder.Services.AddTransient<ChatOptions>(sp => new ChatOptions
        {
            Tools = FunctionRegistry.GetTools(sp).ToList(),
        });

        builder.Services.AddSingleton<IndexBuilder>();
        builder.Services.AddSingleton<RagQuestionService>();
        builder.Services.AddSingleton<TranscriptSplitter>();
        builder.Services.AddSingleton<PromptService>();

    }
}
