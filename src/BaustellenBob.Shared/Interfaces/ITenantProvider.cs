namespace BaustellenBob.Shared.Interfaces;

public interface ITenantProvider
{
    Guid TenantId { get; }
}
