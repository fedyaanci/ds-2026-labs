namespace Contracts;

public sealed record RankCalculationRequested(string TextId);

public static class Messaging
{
    public const string RankQueue = "rank-calculation-requests";
}
