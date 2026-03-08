using BaustellenBob.Domain.Enums;

namespace BaustellenBob.Domain;

public record TierLimit(int MaxProjects, int MaxEmployees, int MaxPhotos, int MaxReports, bool PdfExport);

public static class TierLimits
{
    private static readonly Dictionary<Tier, TierLimit> Limits = new()
    {
        [Tier.Free]     = new(1,   1,     20,   5,    false),
        [Tier.Starter]  = new(10,  5,     1000, int.MaxValue, true),
        [Tier.Pro]      = new(50,  20,    10000, int.MaxValue, true),
        [Tier.Business] = new(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, true),
    };

    public static TierLimit For(Tier tier) => Limits[tier];
}
