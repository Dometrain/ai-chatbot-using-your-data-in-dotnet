using ChatBot.Models;
using Microsoft.Extensions.AI;

namespace ChatBot.Services;

public class RagQuestionService(VectorSearchService vectorSearch, IChatClient client, ChatOptions chatOptions, PromptService promptService)
{
    public async Task<string> AnswerQuestion(string question)
    {
        var searchResults = await vectorSearch.FindTopKChunks(question, 5);

        var systemPrompt = promptService.RagSystemPrompt;

        var userPrompt = $@"User question:
{question}

Retrieved article sections:
{String.Join("\n\n", searchResults.Select(chunk => @$"Title: {chunk.Title}
Section: {chunk.Section}
Part: {chunk.ChunkIndex + 1}
Content: {chunk.Content}
URL:{chunk.SourcePageUrl}"))}
";

        var messages = (new[] {
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, userPrompt)
        }).ToList();

        var response = await client.GetResponseAsync(messages, chatOptions);

        return response.Text;
    }
}