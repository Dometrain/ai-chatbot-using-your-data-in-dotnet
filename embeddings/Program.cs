using System.Text;
using System.Globalization;
using OpenAI.Embeddings;
using Microsoft.Extensions.AI;
using MathNet.Numerics.LinearAlgebra;

/** 
NOTE - You need OPENAI_API_KEY in an environment variable
Run in powershell like this...

$env:OPENAI_API_KEY="XXXXXXX"
dotnet run
**/
class Program
{
    static async Task Main(string[] args)
    {
        var openAiKey = RequireEnv("OPENAI_API_KEY");
        var vectors = new List<(string Word, float[] Vector)>();

        var embedder = new EmbeddingClient(
                model: "text-embedding-3-small",
                apiKey: openAiKey
        ).AsIEmbeddingGenerator();

        var words = new[] { "cat", "mouse", "lion", "tiger", "helicopter", "train", "blue", "carrot", "space" };

        for (var i = 0; i < words.Length; i++)
        {
            var embedding = await embedder.GenerateAsync([words[i]],
                new Microsoft.Extensions.AI.EmbeddingGenerationOptions { Dimensions = 512 });

            var vector = embedding[0].Vector.ToArray();
            vectors.Add((words[i], vector));
        }

        SaveCsv(vectors, "animals.csv");
    }

    #region Helpers

    static string RequireEnv(string key)
    {
        var v = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(v))
            throw new Exception($"Missing env var: {key}");
        return v!;
    }


    static void SaveCsv(
        List<(string Word, float[] Vec)> data,
        string path)
    {
        if (data.Count == 0)
        {
            Console.WriteLine("No vectors to project.");
            return;
        }

        // Build an n x d matrix (double) and mean-center
        int n = data.Count;
        int d = data[0].Vec.Length;

        var X = Matrix<double>.Build.Dense(n, d, (i, j) => data[i].Vec[j]);
        // Mean-center columns
        var means = Vector<double>.Build.Dense(d);
        for (int j = 0; j < d; j++)
        {
            means[j] = X.Column(j).Average();
            for (int i = 0; i < n; i++)
                X[i, j] -= means[j];
        }

        // PCA via SVD of mean-centered X
        // X = U * S * V^T, principal directions = V columns
        var svd = X.Svd(computeVectors: true);
        var V = svd.VT.Transpose(); // d x d

        // Take first two principal components
        var V2 = V.SubMatrix(0, d, 0, 2); // d x 2
        var Y = X * V2; // n x 2

        // Write CSV: id,title,x,y  (culture-invariant)
        using var sw = new StreamWriter(path, false, Encoding.UTF8);
        sw.WriteLine("id,title,x,y");

        for (int i = 0; i < n; i++)
        {
            var x = Y[i, 0];
            var y = Y[i, 1];
            sw.WriteLine($"{CsvEscape(data[i].Word)},{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    static string CsvEscape(string s)
    {
        if (s == null) return "";
        var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        if (needsQuotes)
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    #endregion
}

