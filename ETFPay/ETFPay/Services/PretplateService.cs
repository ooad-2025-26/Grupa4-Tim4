using ETFPay.Data;
using ETFPay.Models;
using Microsoft.EntityFrameworkCore;

namespace ETFPay.Services
{
    public class PretplateService : IPretplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PretplateService> _logger;

        public PretplateService(ApplicationDbContext context, ILogger<PretplateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ExecuteDuePaymentsAsync()
        {
            try
            {
                _logger.LogInformation("Starting scheduled subscription payment execution at {Time}", DateTime.Now);

                var subscriptions = await _context.Predlozak
                    .Where(p => p.Pretplata == true)
                    .ToListAsync();

                if (subscriptions == null || subscriptions.Count == 0)
                {
                    _logger.LogInformation("No active subscriptions found");
                    return;
                }

                int successfulPayments = 0;
                int failedPayments = 0;

                foreach (var subscription in subscriptions)
                {
                    try
                    {
                        if (IsPaymentDue(subscription, subscription.PosljednjePlacanje))
                        {
                            if (await ExecutePaymentAsync(subscription))
                            {
                                successfulPayments++;
                                subscription.PosljednjePlacanje = DateOnly.FromDateTime(DateTime.Now);
                                _context.Predlozak.Update(subscription);
                            }
                            else
                            {
                                failedPayments++;
                                _logger.LogWarning("Payment execution failed for subscription {SubscriptionId}", subscription.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failedPayments++;
                        _logger.LogError(ex, "Error executing payment for subscription {SubscriptionId}", subscription.Id);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Subscription payment execution completed. Successful: {SuccessfulCount}, Failed: {FailedCount}",
                    successfulPayments,
                    failedPayments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in ExecuteDuePaymentsAsync");
            }
        }

        public bool IsPaymentDue(Predlozak subscription, DateOnly PosljednjePlacanje)
        {
            var now = DateOnly.FromDateTime(DateTime.Now);

            return subscription.Period switch
            {
                Period.Dnevno => PosljednjePlacanje.AddDays(1) <= now,
                Period.Sedmicno => PosljednjePlacanje.AddDays(7) <= now,
                Period.Mjesecno => PosljednjePlacanje.AddMonths(1) <= now,
                Period.Godisnje => PosljednjePlacanje.AddYears(1) <= now,
                _ => false
            };
        }

        private async Task<bool> ExecutePaymentAsync(Predlozak subscription)
        {
            try
            {
                var senderAccount = await _context.Racun
                    .FirstOrDefaultAsync(r => r.brojRacuna == subscription.BrojRacuna && r.Aktivan);

                if (senderAccount == null)
                {
                    _logger.LogWarning("Sender account not found for subscription {SubscriptionId}, account: {AccountNumber}",
                        subscription.Id, subscription.BrojRacuna);
                    return false;
                }

                if (senderAccount.Stanje < subscription.Iznos)
                {
                    _logger.LogWarning("Insufficient balance for subscription {SubscriptionId}. Required: {Required}, Available: {Available}",
                        subscription.Id, subscription.Iznos, senderAccount.Stanje);
                    return false;
                }

                var recipientAccount = await _context.Racun
                    .FirstOrDefaultAsync(r => r.brojRacuna == subscription.Primaoc && r.Aktivan);

                if (recipientAccount == null)
                {
                    _logger.LogWarning("Recipient account not found for subscription {SubscriptionId}, account: {AccountNumber}",
                        subscription.Id, subscription.Primaoc);
                    return false;
                }

                var transaction = new Transakcija
                {
                    Id = Guid.NewGuid().ToString(),
                    Posiljaoc = senderAccount.Id,
                    Primaoc = recipientAccount.Id,
                    Iznos = subscription.Iznos,
                    VrijemeTransakcije = DateTime.Now,
                    SvrhaUplate = subscription.SvrhaUplate ?? $"Automatska pretplata - {subscription.Naziv}"
                };

                senderAccount.Stanje -= subscription.Iznos;
                recipientAccount.Stanje += subscription.Iznos;

                _context.Transakcija.Add(transaction);
                _context.Racun.Update(senderAccount);
                _context.Racun.Update(recipientAccount);

                _logger.LogInformation(
                    "Payment executed successfully. Subscription: {SubscriptionId}, Amount: {Amount}, From: {FromAccount}, To: {ToAccount}",
                    subscription.Id, subscription.Iznos, senderAccount.brojRacuna, recipientAccount.brojRacuna);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing payment for subscription {SubscriptionId}", subscription.Id);
                return false;
            }
        }
    }
}
