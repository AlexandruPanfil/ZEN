namespace ZEN.Domain.Entities;

public class Group : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public Guid? MenuId { get; set; }

    public Menu? Menu { get; set; }
    public ICollection<GroupProduct> Products { get; set; } = [];
}