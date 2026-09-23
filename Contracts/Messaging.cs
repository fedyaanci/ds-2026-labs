namespace Contracts;

public sealed record RankCalculationRequested(string TextId);
public sealed record RankCalculated(string TextId, double Rank);
public sealed record SimilarityCalculated(string TextId, double Similarity);

public static class Messaging
{
    public const string RankQueue = "rank-calculation-requests";
    public const string EventsExchange = "valuation-events";
    public const string RankCalculatedRoutingKey = "rank.calculated";
    public const string SimilarityCalculatedRoutingKey = "similarity.calculated";
}
