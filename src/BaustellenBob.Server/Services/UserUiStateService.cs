namespace BaustellenBob.Server.Services;

public sealed class UserUiStateService
{
    public event Action? AvatarChanged;

    public void NotifyAvatarChanged() => AvatarChanged?.Invoke();
}
