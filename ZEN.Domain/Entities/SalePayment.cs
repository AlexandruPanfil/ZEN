using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class SalePayment : BaseEntity
{
    public Guid SaleId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    public Sale Sale { get; set; } = null!;
}
