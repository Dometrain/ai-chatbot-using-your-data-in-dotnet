using System;
using System.Collections.Generic;
using System.Linq;

public record SearchHit(string Id, double Score);

public static class HydeFusion
{
    public static List<SearchHit> ReciprocalRankFusion(
        IEnumerable<IEnumerable<SearchHit>> rankedLists,
        int topK,
        int k = 60)
    {
        // Accumulate RRF scores by document Id
        var scores = new Dictionary<string, double>();

        foreach (var list in rankedLists)
        {
            // Use the index as the rank (1-based)
            int rank = 1;
            foreach (var hit in list)
            {
                // RRF contribution for this list and rank
                double contrib = 1.0 / (k + rank);

                if (scores.TryGetValue(hit.Id, out var existing))
                    scores[hit.Id] = existing + contrib;
                else
                    scores[hit.Id] = contrib;

                rank++;
            }
        }

        // Sort by total RRF score (desc) and take topK
        return scores
            .OrderByDescending(kvp => kvp.Value)
            .Take(topK)
            .Select(kvp => new SearchHit(kvp.Key, kvp.Value)) // Score holds the fused RRF score
            .ToList();
    }


    public static List<SearchHit> MaximalMarginalRelevance(
        IReadOnlyList<SearchHit> candidates,
        Func<string, string, double> docSim,
        double lambda = 0.7,
        int topK = 5)
    {
        if (candidates == null || candidates.Count == 0 || topK <= 0)
            return new List<SearchHit>();

        // Work on a mutable pool of remaining items; we only need Id + query-sim
        var remaining = candidates
            .DistinctBy(c => c.Id)            // safety: drop accidental duplicates by Id
            .OrderByDescending(c => c.Score)  // good initialization order (optional)
            .ToList();

        var selected = new List<SearchHit>(capacity: Math.Min(topK, remaining.Count));

        while (selected.Count < topK && remaining.Count > 0)
        {
            SearchHit best = null!;
            double bestMmr = double.NegativeInfinity;

            foreach (var cand in remaining)
            {
                // Diversity penalty: max similarity to any already selected doc
                double redundancy = 0.0;
                if (selected.Count > 0)
                {
                    redundancy = selected
                        .Select(s => docSim(cand.Id, s.Id))
                        .DefaultIfEmpty(0.0)
                        .Max();
                }

                // MMR score combines query relevance (cand.Score) and redundancy
                double mmr = lambda * cand.Score - (1.0 - lambda) * redundancy;

                if (mmr > bestMmr)
                {
                    bestMmr = mmr;
                    best = cand;
                }
            }

            selected.Add(best);
            remaining.RemoveAll(c => c.Id == best.Id);
        }

        return selected;
    }
}
