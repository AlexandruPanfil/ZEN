using ZEN.Application.DTOs.CashDeskMenu;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ZEN.Infrastructure.Services;

public class MenuService : IMenuService
{
    private readonly ZenDbContext _dbContext;

    public MenuService(ZenDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<MenuSummaryDto>> GetAllSummariesAsync(CancellationToken cancellationToken = default, bool includeDeleted = false)
    {
        var query = includeDeleted
            ? _dbContext.Menus.IgnoreQueryFilters()
            : _dbContext.Menus;

        return await query
            .OrderBy(m => m.Name)
            .Select(m => new MenuSummaryDto(m.Id, m.Name, m.StoreId, m.Version, m.IsDeleted))
            .ToListAsync(cancellationToken);
    }

    public async Task<MenuDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var menu = await _dbContext.Menus
            .Include(m => m.Products).ThenInclude(mp => mp.Product)
            .Include(m => m.Groups).ThenInclude(mg => mg.Group).ThenInclude(g => g.Products).ThenInclude(gp => gp.Product)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

        return menu == null ? null : ToDto(menu);
    }

    public async Task<MenuDto> CreateAsync(CreateMenuRequest request, CancellationToken cancellationToken = default)
    {
        var menu = new Menu
        {
            Id = request.Id ?? Guid.NewGuid(),
            Name = request.Name,
            StoreId = request.StoreId
        };

        _dbContext.Menus.Add(menu);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(menu.Id, cancellationToken))!;
    }

    public async Task<MenuDto> UpdateAsync(UpdateMenuRequest request, CancellationToken cancellationToken = default)
    {
        var menu = await _dbContext.Menus.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (menu == null)
            throw new KeyNotFoundException($"Menu with ID {request.Id} not found.");

        if (menu.IsDeleted || (request.BaseVersion != 0 && request.BaseVersion != menu.Version))
            throw new ZEN.Application.Services.Exceptions.SyncConflictException(menu.Id, request.BaseVersion, menu.Version);

        menu.Name = request.Name;
        menu.StoreId = request.StoreId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(menu.Id, cancellationToken))!;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default, long baseVersion = 0)
    {
        var menu = await _dbContext.Menus.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (menu == null)
            return false;
        if (menu.IsDeleted || (baseVersion != 0 && baseVersion != menu.Version))
            throw new ZEN.Application.Services.Exceptions.SyncConflictException(menu.Id, baseVersion, menu.Version);

        _dbContext.Menus.Remove(menu);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<FlatMenuItemDto>> GetFlatItemsAsync(Guid menuId, CancellationToken cancellationToken = default)
    {
        var menu = await _dbContext.Menus
            .Include(m => m.Products).ThenInclude(mp => mp.Product).ThenInclude(p => p.Tax)
            .Include(m => m.Groups).ThenInclude(mg => mg.Group).ThenInclude(g => g.Products).ThenInclude(gp => gp.Product).ThenInclude(p => p.Tax)
            .FirstOrDefaultAsync(m => m.Id == menuId && !m.IsDeleted, cancellationToken);

        if (menu == null)
            return Enumerable.Empty<FlatMenuItemDto>();

        var items = new List<FlatMenuItemDto>();

        foreach (var mp in menu.Products.Where(p => p.Product != null).OrderBy(p => p.Position))
        {
            items.Add(new FlatMenuItemDto(mp.ProductId, mp.Product!.Name, mp.Product.SalePrice, null, null, mp.Product.Tax?.Rate ?? 0m));
        }

        foreach (var mg in menu.Groups.Where(g => g.Group != null).OrderBy(g => g.Position))
        {
            var group = mg.Group!;
            foreach (var gp in group.Products.Where(p => p.Product != null).OrderBy(p => p.Position))
            {
                items.Add(new FlatMenuItemDto(gp.ProductId, gp.Product!.Name, gp.Product.SalePrice, group.Id, group.Name, gp.Product.Tax?.Rate ?? 0m));
            }
        }

        return items;
    }

    public async Task<MenuDto> AddProductAsync(AddProductToMenuRequest request, CancellationToken cancellationToken = default)
    {
        var menuExists = await _dbContext.Menus.AnyAsync(m => m.Id == request.MenuId && !m.IsDeleted, cancellationToken);
        if (!menuExists)
            throw new KeyNotFoundException($"Menu with ID {request.MenuId} not found.");

        var productExists = await _dbContext.Products.AnyAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);
        if (!productExists)
            throw new KeyNotFoundException($"Product with ID {request.ProductId} not found.");

        var alreadyInMenu = await _dbContext.MenuProducts.AnyAsync(
            mp => mp.MenuId == request.MenuId && mp.ProductId == request.ProductId, cancellationToken);
        if (alreadyInMenu)
            return (await GetByIdAsync(request.MenuId, cancellationToken))!;

        var menuProduct = new MenuProduct
        {
            MenuId = request.MenuId,
            ProductId = request.ProductId,
            Position = request.Position
        };

        _dbContext.MenuProducts.Add(menuProduct);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(request.MenuId, cancellationToken))!;
    }

    public async Task<MenuDto> AddGroupAsync(AddGroupToMenuRequest request, CancellationToken cancellationToken = default)
    {
        var menuExists = await _dbContext.Menus.AnyAsync(m => m.Id == request.MenuId && !m.IsDeleted, cancellationToken);
        if (!menuExists)
            throw new KeyNotFoundException($"Menu with ID {request.MenuId} not found.");

        var groupExists = await _dbContext.Groups.AnyAsync(g => g.Id == request.GroupId && !g.IsDeleted, cancellationToken);
        if (!groupExists)
            throw new KeyNotFoundException($"Group with ID {request.GroupId} not found.");

        var alreadyInMenu = await _dbContext.MenuGroups.AnyAsync(
            mg => mg.MenuId == request.MenuId && mg.GroupId == request.GroupId, cancellationToken);
        if (alreadyInMenu)
            return (await GetByIdAsync(request.MenuId, cancellationToken))!;

        var menuGroup = new MenuGroup
        {
            MenuId = request.MenuId,
            GroupId = request.GroupId,
            Position = request.Position
        };

        _dbContext.MenuGroups.Add(menuGroup);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(request.MenuId, cancellationToken))!;
    }

    public async Task<bool> RemoveMenuProductAsync(Guid menuProductId, CancellationToken cancellationToken = default)
    {
        var menuProduct = await _dbContext.MenuProducts.FindAsync([menuProductId], cancellationToken);
        if (menuProduct == null)
            return false;

        _dbContext.MenuProducts.Remove(menuProduct);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveMenuGroupAsync(Guid menuGroupId, CancellationToken cancellationToken = default)
    {
        var menuGroup = await _dbContext.MenuGroups.FindAsync([menuGroupId], cancellationToken);
        if (menuGroup == null)
            return false;

        _dbContext.MenuGroups.Remove(menuGroup);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<GroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = await _dbContext.Groups
            .Include(g => g.Products).ThenInclude(gp => gp.Product)
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.DisplayOrder)
            .ToListAsync(cancellationToken);

        return groups.Select(g => ToGroupDto(g));
    }

    public async Task<GroupDto> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = new Group
        {
            Name = request.Name,
            DisplayOrder = request.DisplayOrder,
            MenuId = request.MenuId
        };

        _dbContext.Groups.Add(group);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToGroupDto(group);
    }

    public async Task<GroupDto> UpdateGroupAsync(UpdateGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await _dbContext.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (group == null)
            throw new KeyNotFoundException($"Group with ID {request.Id} not found.");

        group.Name = request.Name;
        group.DisplayOrder = request.DisplayOrder;
        group.MenuId = request.MenuId;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await _dbContext.Groups
            .Include(g => g.Products).ThenInclude(gp => gp.Product)
            .FirstAsync(g => g.Id == request.Id, cancellationToken);

        return ToGroupDto(reloaded);
    }

    public async Task<bool> DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await _dbContext.Groups.FindAsync([groupId], cancellationToken);
        if (group == null)
            return false;

        var groupProducts = _dbContext.GroupProducts.Where(gp => gp.GroupId == groupId);
        _dbContext.GroupProducts.RemoveRange(groupProducts);

        var menuGroups = _dbContext.MenuGroups.Where(mg => mg.GroupId == groupId);
        _dbContext.MenuGroups.RemoveRange(menuGroups);

        _dbContext.Groups.Remove(group);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AddProductToGroupAsync(AddProductToGroupRequest request, CancellationToken cancellationToken = default)
    {
        var groupExists = await _dbContext.Groups.AnyAsync(g => g.Id == request.GroupId && !g.IsDeleted, cancellationToken);
        if (!groupExists)
            throw new KeyNotFoundException($"Group with ID {request.GroupId} not found.");

        var productExists = await _dbContext.Products.AnyAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);
        if (!productExists)
            throw new KeyNotFoundException($"Product with ID {request.ProductId} not found.");

        var alreadyInGroup = await _dbContext.GroupProducts.AnyAsync(
            gp => gp.GroupId == request.GroupId && gp.ProductId == request.ProductId, cancellationToken);
        if (alreadyInGroup)
            return false;

        var groupProduct = new GroupProduct
        {
            GroupId = request.GroupId,
            ProductId = request.ProductId,
            Position = request.Position
        };

        _dbContext.GroupProducts.Add(groupProduct);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveProductFromGroupAsync(Guid groupProductId, CancellationToken cancellationToken = default)
    {
        var groupProduct = await _dbContext.GroupProducts.FindAsync([groupProductId], cancellationToken);
        if (groupProduct == null)
            return false;

        _dbContext.GroupProducts.Remove(groupProduct);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static MenuDto ToDto(Menu menu)
    {
        var products = menu.Products.Where(p => p.Product != null).OrderBy(p => p.Position)
            .Select(p => new MenuProductDto(
                p.Id,
                p.ProductId,
                p.Product!.Name,
                p.Product.SalePrice,
                p.Position));

        var groups = menu.Groups.Where(g => g.Group != null).OrderBy(g => g.Position)
            .Select(g => new MenuGroupDto(
                g.Id,
                g.GroupId,
                g.Group!.Name,
                g.Position,
                g.Group.Products.Where(gp => gp.Product != null).OrderBy(gp => gp.Position)
                    .Select(gp => new GroupProductDto(
                        gp.ProductId,
                        gp.Product!.Name,
                        gp.Product.SalePrice,
                        gp.Position))));

        return new MenuDto(menu.Id, menu.Name, menu.StoreId, products, groups);
    }

    private static GroupDto ToGroupDto(Group group)
    {
        return new GroupDto(
            group.Id,
            group.Name,
            group.DisplayOrder,
            group.MenuId,
            group.Products.Where(gp => gp.Product != null).OrderBy(gp => gp.Position)
                .Select(gp => new GroupProductDto(
                    gp.ProductId,
                    gp.Product!.Name,
                    gp.Product.SalePrice,
                    gp.Position)));
    }
}
