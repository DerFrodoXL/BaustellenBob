using BaustellenBob.Domain.Enums;

namespace BaustellenBob.Domain.Entities;

public class Tenant
{
    public const string DefaultWorkingDays = "1,2,3,4,5";

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Tier Tier { get; set; }
    public string? LogoPath { get; set; }
    public byte[]? LogoData { get; set; }
    public string? LogoContentType { get; set; }

    // Stripe
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public string WorkingDays { get; set; } = DefaultWorkingDays;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
