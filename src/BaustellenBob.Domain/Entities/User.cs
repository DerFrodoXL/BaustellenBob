using BaustellenBob.Domain.Enums;

namespace BaustellenBob.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
