namespace BaustellenBob.Shared.Interfaces;

public interface ICurrentUserProvider
{
    Guid UserId { get; }
    string UserName { get; }
    string Role { get; }
}
