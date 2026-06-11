using ETFPay.Models;

namespace ETFPay.Services
{
    public interface IPretplateService
    {
        // Izvrsava placanja za subskripcije cije je vrijeme za placanje
        Task ExecuteDuePaymentsAsync();

        // Vraca true ako je vrijeme za placanje
        bool IsPaymentDue(Predlozak subscription, DateOnly PosljednjePlacanje);
    }
}
