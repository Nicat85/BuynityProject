using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }

        public string Currency { get; set; } = "AZN";
        public string? Provider { get; set; }
        public string? ProviderPaymentId { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    }
}
