namespace BaustellenBob.Application.Interfaces;

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterAsync(string companyName, string adminName, string email, string password);
}

public class RegistrationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
}
