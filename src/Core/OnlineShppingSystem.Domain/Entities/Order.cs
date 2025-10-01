using OnlineSohppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;

namespace OnlineShppingSystem.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid BuyerId { get; set; }
        public AppUser Buyer { get; set; } = null!;

        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Payment Payment { get; set; } = null!;
        public CourierAssignment? CourierAssignment { get; set; }
    }
}
