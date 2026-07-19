namespace ZEN.Domain.Entities;

public class Menu : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? StoreId { get; set; }

    public Store? Store { get; set; }
    public ICollection<MenuProduct> Products { get; set; } = [];
    public ICollection<MenuGroup> Groups { get; set; } = [];
    public ICollection<CashDesk> CashDesks { get; set; } = [];
}

public class MenuProduct : BaseEntity
{
    public Guid MenuId { get; set; }
    public Guid ProductId { get; set; }
    public int Position { get; set; }

    public Menu Menu { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class MenuGroup : BaseEntity
{
    public Guid MenuId { get; set; }
    public Guid GroupId { get; set; }
    public int Position { get; set; }

    public Menu Menu { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
