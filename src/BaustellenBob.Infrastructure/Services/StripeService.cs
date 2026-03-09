using BaustellenBob.Application.Interfaces;
using BaustellenBob.Domain.Enums;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace BaustellenBob.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;

    // Maps each tier to a Stripe Price ID (configured in appsettings)
    private static readonly Dictionary<Tier, string> TierConfigKeys = new()
    {
        [Tier.Starter] = "Stripe:Prices:Starter",
        [Tier.Pro] = "Stripe:Prices:Pro",
        [Tier.Business] = "Stripe:Prices:Business",
    };

    public StripeService(AppDbContext db, ITenantProvider tenantProvider, IConfiguration configuration, ILogger<StripeService> logger)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _configuration = configuration;
        _logger = logger;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(Tier targetTier, string successUrl, string cancelUrl)
    {
        if (targetTier == Tier.Free)
            throw new InvalidOperationException("Free-Tier erfordert kein Abo.");

        if (!TierConfigKeys.TryGetValue(targetTier, out var configKey))
            throw new InvalidOperationException($"Kein Preis konfiguriert für {targetTier}.");

        var priceId = _configuration[configKey]
            ?? throw new InvalidOperationException($"Stripe Price-ID fehlt in Konfiguration: {configKey}");

        var tenant = await _db.Tenants.FindAsync(_tenantProvider.TenantId)
            ?? throw new InvalidOperationException("Tenant nicht gefunden.");

        // Ensure Stripe customer exists
        var customerId = await EnsureStripeCustomerAsync(tenant);

        var options = new SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new() { Price = priceId, Quantity = 1 }
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["TenantId"] = tenant.Id.ToString(),
                ["TargetTier"] = targetTier.ToString()
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }

    public async Task<string> CreateBillingPortalSessionAsync(string returnUrl)
    {
        var tenant = await _db.Tenants.FindAsync(_tenantProvider.TenantId)
            ?? throw new InvalidOperationException("Tenant nicht gefunden.");

        if (string.IsNullOrEmpty(tenant.StripeCustomerId))
            throw new InvalidOperationException("Kein Stripe-Konto vorhanden. Bitte zuerst ein Abo abschließen.");

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = tenant.StripeCustomerId,
            ReturnUrl = returnUrl
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }

    public async Task<bool> HandleWebhookAsync(string json, string stripeSignature)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogError("Stripe:WebhookSecret ist nicht konfiguriert.");
            return false;
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe Webhook Signaturprüfung fehlgeschlagen.");
            return false;
        }

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
                await HandleCheckoutCompleted(stripeEvent);
                break;

            case EventTypes.CustomerSubscriptionUpdated:
                await HandleSubscriptionUpdated(stripeEvent);
                break;

            case EventTypes.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeleted(stripeEvent);
                break;

            default:
                _logger.LogInformation("Unbehandelter Stripe Event: {Type}", stripeEvent.Type);
                break;
        }

        return true;
    }

    public async Task<SubscriptionInfo> GetSubscriptionInfoAsync()
    {
        var tenant = await _db.Tenants.FindAsync(_tenantProvider.TenantId)
            ?? throw new InvalidOperationException("Tenant nicht gefunden.");

        if (string.IsNullOrEmpty(tenant.StripeSubscriptionId))
        {
            return new SubscriptionInfo(tenant.Tier, null, null, null, CanManage: !string.IsNullOrEmpty(tenant.StripeCustomerId));
        }

        try
        {
            var subService = new Stripe.SubscriptionService();
            var subscription = await subService.GetAsync(tenant.StripeSubscriptionId);
            return new SubscriptionInfo(
                tenant.Tier,
                subscription.Id,
                subscription.Status,
                subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd,
                CanManage: true);
        }
        catch (StripeException)
        {
            return new SubscriptionInfo(tenant.Tier, tenant.StripeSubscriptionId, "unknown", null, CanManage: true);
        }
    }

    private async Task<string> EnsureStripeCustomerAsync(Domain.Entities.Tenant tenant)
    {
        if (!string.IsNullOrEmpty(tenant.StripeCustomerId))
            return tenant.StripeCustomerId;

        var options = new Stripe.CustomerCreateOptions
        {
            Name = tenant.Name,
            Metadata = new Dictionary<string, string>
            {
                ["TenantId"] = tenant.Id.ToString()
            }
        };

        var custService = new Stripe.CustomerService();
        var customer = await custService.CreateAsync(options);

        tenant.StripeCustomerId = customer.Id;
        await _db.SaveChangesAsync();

        return customer.Id;
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session is null) return;

        var tenantIdStr = session.Metadata.GetValueOrDefault("TenantId");
        var targetTierStr = session.Metadata.GetValueOrDefault("TargetTier");

        if (!Guid.TryParse(tenantIdStr, out var tenantId) || !Enum.TryParse<Tier>(targetTierStr, out var targetTier))
        {
            _logger.LogWarning("Checkout session metadata ungültig: TenantId={TenantId}, TargetTier={TargetTier}", tenantIdStr, targetTierStr);
            return;
        }

        var tenant = await _db.Tenants.FindAsync(tenantId);
        if (tenant is null)
        {
            _logger.LogWarning("Tenant {TenantId} nicht gefunden nach Checkout.", tenantId);
            return;
        }

        tenant.Tier = targetTier;
        tenant.StripeSubscriptionId = session.SubscriptionId;
        if (string.IsNullOrEmpty(tenant.StripeCustomerId))
            tenant.StripeCustomerId = session.CustomerId;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Tenant {TenantId} auf Tier {Tier} aktualisiert (Checkout).", tenantId, targetTier);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null) return;

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.StripeSubscriptionId == subscription.Id);
        if (tenant is null)
        {
            _logger.LogWarning("Kein Tenant für Subscription {SubscriptionId} gefunden.", subscription.Id);
            return;
        }

        // Map Stripe Price to Tier
        var priceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
        if (priceId is not null)
        {
            var newTier = MapPriceIdToTier(priceId);
            if (newTier is not null)
            {
                tenant.Tier = newTier.Value;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Tenant {TenantId} Tier aktualisiert auf {Tier} (Subscription Update).", tenant.Id, newTier);
            }
        }
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null) return;

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.StripeSubscriptionId == subscription.Id);
        if (tenant is null) return;

        tenant.Tier = Tier.Free;
        tenant.StripeSubscriptionId = null;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Tenant {TenantId} auf Free herabgestuft (Subscription gelöscht).", tenant.Id);
    }

    private Tier? MapPriceIdToTier(string priceId)
    {
        foreach (var (tier, configKey) in TierConfigKeys)
        {
            if (_configuration[configKey] == priceId)
                return tier;
        }
        return null;
    }
}
