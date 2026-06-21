namespace ZEN.Domain.Entities;

public class Currency : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; } = 1m;
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
