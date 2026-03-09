using BaustellenBob.Domain.Enums;

namespace BaustellenBob.Application.Interfaces;

public interface IStripeService
{
    /// <summary>Creates a Stripe Checkout session and returns the URL to redirect to.</summary>
    Task<string> CreateCheckoutSessionAsync(Tier targetTier, string successUrl, string cancelUrl);

    /// <summary>Creates a Stripe Billing Portal session and returns the URL.</summary>
    Task<string> CreateBillingPortalSessionAsync(string returnUrl);

    /// <summary>Processes an incoming Stripe webhook event. Returns true if handled.</summary>
    Task<bool> HandleWebhookAsync(string json, string stripeSignature);

    /// <summary>Returns the current subscription info for the tenant.</summary>
    Task<SubscriptionInfo> GetSubscriptionInfoAsync();
}

public record SubscriptionInfo(
    Tier CurrentTier,
    string? StripeSubscriptionId,
    string? Status,
    DateTime? CurrentPeriodEnd,
    bool CanManage);
