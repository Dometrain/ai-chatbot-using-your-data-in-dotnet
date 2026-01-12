# Let's Build It: AI Chatbot Using Your Data in .NET

## Back End

> The project uses [.NET version 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) so install the .NET SDK in order to run locally.

### Getting Your API Keys

The projects on this course integrate with a two third party services by using API keys.  You will need to create accounts on these services and create API keys.

#### OpenAI

You can create an API key for OpenAI on [platform.openai.com/api-keys](https://platform.openai.com/api-keys) after creating an account with OpenAI.  This API key will be placed into an environment variable called `OPENAI_API_KEY` in your [appsettings.json](./appsettings.json):

#### Pinecone

Pinecone is a cloud-based vector database service designed for fast and scalable similarity search.  Sign up for the [Starter](https://www.pinecone.io/pricing/) plan (free at time of writing) and create an API key using this guide [https://docs.pinecone.io/guides/projects/manage-api-keys](https://docs.pinecone.io/guides/projects/manage-api-keys).

This API key will be placed into an environment variable called `PINECONE_API_KEY` in your [appsettings.json](./appsettings.json)

### Environment Variables

Your final [appsettings.json](./appsettings.json) should look like this:

```json
{
  ...
  "Keys": {
    "OPENAI_API_KEY": "<< your api key >>>",
    "PINECONE_API_KEY": "<< your api key >>>"
  }
}
```

### Wikipedia Client

Be good netizen and put your email address into [WikipediaClient.cs](./Services/WikipediaClient.cs#L18) when calling the Wikipedia api.

```csharp
WikipediaHttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(contact:you@example.com)"));
```