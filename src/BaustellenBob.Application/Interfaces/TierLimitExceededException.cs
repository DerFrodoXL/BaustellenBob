namespace BaustellenBob.Application.Interfaces;

public class TierLimitExceededException : Exception
{
    public TierLimitExceededException(string resource, int limit)
        : base($"Tier-Limit erreicht: max. {limit} {resource}.")
    {
        Resource = resource;
        Limit = limit;
    }

    public string Resource { get; }
    public int Limit { get; }
}
