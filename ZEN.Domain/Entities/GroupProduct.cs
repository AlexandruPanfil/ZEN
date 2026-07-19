namespace ZEN.Domain.Entities;

public class GroupProduct : BaseEntity
{
    public Guid GroupId { get; set; }
    public Guid ProductId { get; set; }
    public int Position { get; set; }

    public Group Group { get; set; } = null!;
    public Product Product { get; set; } = null!;
}