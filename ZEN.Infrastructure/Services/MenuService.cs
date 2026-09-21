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
            .Include(m => m.Products).ThenInclude(mp => mp.Product).ThenInclude(p => p.Tax)
            .Include(m => m.Groups).ThenInclude(mg => mg.Products).ThenInclude(mgp => mgp.Product).ThenInclude(p => p.Tax)
            .Include(m => m.GroupReferences).ThenInclude(mgr => mgr.Group).ThenInclude(g => g.Products).ThenInclude(gp => gp.Product).ThenInclude(p => p.Tax)
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
            .Include(m => m.Groups).ThenInclude(mg => mg.Products).ThenInclude(mgp => mgp.Product).ThenInclude(p => p.Tax)
            .Include(m => m.GroupReferences).ThenInclude(mgr => mgr.Group).ThenInclude(g => g.Products).ThenInclude(gp => gp.Product).ThenInclude(p => p.Tax)
            .FirstOrDefaultAsync(m => m.Id == menuId && !m.IsDeleted, cancellationToken);

        if (menu == null)
            return Enumerable.Empty<FlatMenuItemDto>();

        var items = new List<FlatMenuItemDto>();

        foreach (var mp in menu.Products.Where(p => p.Product != null).OrderBy(p => p.Position))
        {
            items.Add(new FlatMenuItemDto(mp.ProductId, mp.Product!.Name, mp.Product.SalePrice, null, null, mp.Product.Tax?.Rate ?? 0m));
        }

        foreach (var mg in menu.Groups.Where(g => g.Products.Any(p => p.Product != null)).OrderBy(g => g.Position))
        {
            foreach (var mgp in mg.Products.Where(p => p.Product != null).OrderBy(p => p.Position))
            {
                items.Add(new FlatMenuItemDto(mgp.ProductId, mgp.Product!.Name, mgp.Product.SalePrice, mg.Id, mg.Name, mgp.Product.Tax?.Rate ?? 0m));
            }
        }

        foreach (var mgr in menu.GroupReferences.Where(g => g.Group != null).OrderBy(g => g.Position))
        {
            var group = mgr.Group!;
            foreach (var gp in group.Products.Where(p => p.Product != null).OrderBy(p => p.Position))
            {
                items.Add(new FlatMenuItemDto(gp.ProductId, gp.Product!.Name, gp.Product.SalePrice, group.Id, group.Name, gp.Product.Tax?.Rate ?? 0m));
            }
        }

        return items;
    }

public async Task<AddProductResult> AddProductAsync(AddProductToMenuRequest request, CancellationToken cancellationToken = default)
    {
        var menuExists = await _dbContext.Menus.AnyAsync(m => m.Id == request.MenuId && !m.IsDeleted, cancellationToken);
        if (!menuExists)
            throw new KeyNotFoundException($"Menu with ID {request.MenuId} not found.");

        var productExists = await _dbContext.Products.AnyAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);
        if (!productExists)
            throw new KeyNotFoundException($"Product with ID {request.ProductId} not found.");

        // Check if product already exists in menu root (including soft-deleted)
        var existingRootProduct = await _dbContext.MenuProducts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(mp => mp.MenuId == request.MenuId && mp.ProductId == request.ProductId, cancellationToken);
        if (existingRootProduct != null)
        {
            if (existingRootProduct.IsDeleted)
            {
                // Restore soft-deleted record
                existingRootProduct.IsDeleted = false;
                existingRootProduct.Position = request.Position;
                existingRootProduct.UpdatedAt = DateTime.UtcNow;
                _dbContext.MenuProducts.Update(existingRootProduct);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return new AddProductResult(AddProductAction.Skipped, request.ProductId);
        }

        // Check if product already exists in any group of the same menu (including soft-deleted)
        var groupIds = await _dbContext.MenuGroups
            .IgnoreQueryFilters()
            .Where(g => g.MenuId == request.MenuId)
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);
        
        MenuGroupProduct? existingGroupProduct = null;
        if (groupIds.Count > 0)
        {
            existingGroupProduct = await _dbContext.MenuGroupProducts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(mgp => groupIds.Contains(mgp.MenuGroupId) && mgp.ProductId == request.ProductId, cancellationToken);
        }
        
        if (existingGroupProduct != null)
        {
            // MOVE: Remove from group and add to root
            _dbContext.MenuGroupProducts.Remove(existingGroupProduct);
            
            var menuProduct = new MenuProduct
            {
                MenuId = request.MenuId,
                ProductId = request.ProductId,
                Position = request.Position
            };

            _dbContext.MenuProducts.Add(menuProduct);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            return new AddProductResult(AddProductAction.MovedFromGroup, request.ProductId);
        }

        // Add new product to root
        var newMenuProduct = new MenuProduct
        {
            MenuId = request.MenuId,
            ProductId = request.ProductId,
            Position = request.Position
        };

        _dbContext.MenuProducts.Add(newMenuProduct);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return new AddProductResult(AddProductAction.Added, request.ProductId);
    }

    public async Task<MenuDto> AddGroupAsync(AddGroupToMenuRequest request, CancellationToken cancellationToken = default)
    {
        var menuExists = await _dbContext.Menus.AnyAsync(m => m.Id == request.MenuId && !m.IsDeleted, cancellationToken);
        if (!menuExists)
            throw new KeyNotFoundException($"Menu with ID {request.MenuId} not found.");

        var groupExists = await _dbContext.Groups.AnyAsync(g => g.Id == request.GroupId && !g.IsDeleted, cancellationToken);
        if (!groupExists)
            throw new KeyNotFoundException($"Group with ID {request.GroupId} not found.");

        var alreadyInMenu = await _dbContext.MenuGroupReferences.AnyAsync(
            mg => mg.MenuId == request.MenuId && mg.GroupId == request.GroupId, cancellationToken);
        if (alreadyInMenu)
            return (await GetByIdAsync(request.MenuId, cancellationToken))!;

        var menuGroupRef = new MenuGroupReference
        {
            MenuId = request.MenuId,
            GroupId = request.GroupId,
            Position = request.Position
        };

        _dbContext.MenuGroupReferences.Add(menuGroupRef);
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

    public async Task<bool> RemoveMenuGroupReferenceAsync(Guid menuGroupId, CancellationToken cancellationToken = default)
    {
        var menuGroup = await _dbContext.MenuGroupReferences.FindAsync([menuGroupId], cancellationToken);
        if (menuGroup == null)
            return false;

        _dbContext.MenuGroupReferences.Remove(menuGroup);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MenuDto> AddMenuGroupAsync(CreateMenuGroupRequest request, CancellationToken cancellationToken = default)
    {
        var menuExists = await _dbContext.Menus.AnyAsync(m => m.Id == request.MenuId && !m.IsDeleted, cancellationToken);
        if (!menuExists)
            throw new KeyNotFoundException($"Menu with ID {request.MenuId} not found.");

        var menuGroup = new MenuGroup
        {
            MenuId = request.MenuId,
            Name = request.Name,
            Position = request.Position
        };

        _dbContext.MenuGroups.Add(menuGroup);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(request.MenuId, cancellationToken))!;
    }

    public async Task<MenuDto> UpdateMenuGroupAsync(UpdateMenuGroupRequest request, CancellationToken cancellationToken = default)
    {
        var menuGroup = await _dbContext.MenuGroups.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (menuGroup == null)
            throw new KeyNotFoundException($"MenuGroup with ID {request.Id} not found.");

        menuGroup.Name = request.Name;
        menuGroup.Position = request.Position;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(menuGroup.MenuId, cancellationToken))!;
    }

    public async Task<bool> RemoveMenuGroupAsync(Guid menuGroupId, CancellationToken cancellationToken = default)
    {
        var menuGroup = await _dbContext.MenuGroups.FindAsync([menuGroupId], cancellationToken);
        if (menuGroup == null)
            return false;

        var groupProducts = _dbContext.MenuGroupProducts.Where(mgp => mgp.MenuGroupId == menuGroupId);
        _dbContext.MenuGroupProducts.RemoveRange(groupProducts);

        _dbContext.MenuGroups.Remove(menuGroup);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

public async Task<AddProductResult> AddProductToMenuGroupAsync(AddProductToMenuGroupRequest request, CancellationToken cancellationToken = default)
        {
            var menuGroup = await _dbContext.MenuGroups
                .Include(g => g.Menu)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(g => g.Id == request.MenuGroupId, cancellationToken);
            
            if (menuGroup == null)
                throw new KeyNotFoundException($"MenuGroup with ID {request.MenuGroupId} not found.");
            
            if (menuGroup.IsDeleted)
                throw new KeyNotFoundException($"MenuGroup with ID {request.MenuGroupId} is deleted.");
            
            // Validate MenuGroup belongs to a valid menu
            if (menuGroup.Menu == null || menuGroup.Menu.IsDeleted)
                throw new InvalidOperationException($"MenuGroup with ID {request.MenuGroupId} does not belong to a valid menu.");

            var productExists = await _dbContext.Products.AnyAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);
            if (!productExists)
                throw new KeyNotFoundException($"Product with ID {request.ProductId} not found.");

            var menuId = menuGroup.MenuId;

            // Check if product already exists in this group (including soft-deleted)
            var existingInGroup = await _dbContext.MenuGroupProducts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(mgp => mgp.MenuGroupId == request.MenuGroupId && mgp.ProductId == request.ProductId, cancellationToken);
            if (existingInGroup != null)
            {
                if (existingInGroup.IsDeleted)
                {
                    // Restore soft-deleted record
                    existingInGroup.IsDeleted = false;
                    existingInGroup.Position = request.Position;
                    existingInGroup.UpdatedAt = DateTime.UtcNow;
                    _dbContext.MenuGroupProducts.Update(existingInGroup);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                return new AddProductResult(AddProductAction.Skipped, request.ProductId);
            }

            // Check if product already exists in menu root (including soft-deleted)
            var existingInRoot = await _dbContext.MenuProducts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(mp => mp.MenuId == menuId && mp.ProductId == request.ProductId, cancellationToken);
            if (existingInRoot != null)
            {
                // MOVE: Remove from root and add to group
                _dbContext.MenuProducts.Remove(existingInRoot);
                
                var menuGroupProduct = new MenuGroupProduct
                {
                    MenuGroupId = request.MenuGroupId,
                    ProductId = request.ProductId,
                    Position = request.Position
                };

                _dbContext.MenuGroupProducts.Add(menuGroupProduct);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                return new AddProductResult(AddProductAction.MovedFromRoot, request.ProductId);
            }

            // Check if product already exists in another group of the same menu (including soft-deleted)
            var otherGroupIds = await _dbContext.MenuGroups
                .IgnoreQueryFilters()
                .Where(g => g.MenuId == menuId && g.Id != request.MenuGroupId)
                .Select(g => g.Id)
                .ToListAsync(cancellationToken);
            
            MenuGroupProduct? existingInOtherGroup = null;
            if (otherGroupIds.Count > 0)
            {
                existingInOtherGroup = await _dbContext.MenuGroupProducts
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(mgp => otherGroupIds.Contains(mgp.MenuGroupId) && mgp.ProductId == request.ProductId, cancellationToken);
            }
            
            if (existingInOtherGroup != null)
            {
                // MOVE: Remove from other group and add to target group
                _dbContext.MenuGroupProducts.Remove(existingInOtherGroup);
                
                var menuGroupProduct = new MenuGroupProduct
                {
                    MenuGroupId = request.MenuGroupId,
                    ProductId = request.ProductId,
                    Position = request.Position
                };

                _dbContext.MenuGroupProducts.Add(menuGroupProduct);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                return new AddProductResult(AddProductAction.MovedFromGroup, request.ProductId);
            }

            // Add new product to group
            var newMenuGroupProduct = new MenuGroupProduct
            {
                MenuGroupId = request.MenuGroupId,
                ProductId = request.ProductId,
                Position = request.Position
            };

            _dbContext.MenuGroupProducts.Add(newMenuGroupProduct);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            return new AddProductResult(AddProductAction.Added, request.ProductId);
        }

    public async Task<bool> RemoveProductFromMenuGroupAsync(Guid menuGroupProductId, CancellationToken cancellationToken = default)
    {
        var menuGroupProduct = await _dbContext.MenuGroupProducts.FindAsync([menuGroupProductId], cancellationToken);
        if (menuGroupProduct == null)
            return false;

        _dbContext.MenuGroupProducts.Remove(menuGroupProduct);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MenuDto> ReorderMenuGroupsAsync(ReorderMenuGroupsRequest request, CancellationToken cancellationToken = default)
    {
        var menuExists = await _dbContext.Menus.AnyAsync(m => m.Id == request.MenuId && !m.IsDeleted, cancellationToken);
        if (!menuExists)
            throw new KeyNotFoundException($"Menu with ID {request.MenuId} not found.");

        var groupIds = request.GroupIdsInOrder.ToList();
        var groups = await _dbContext.MenuGroups
            .Where(g => g.MenuId == request.MenuId && groupIds.Contains(g.Id))
            .ToListAsync(cancellationToken);

        for (int i = 0; i < groupIds.Count; i++)
        {
            var group = groups.FirstOrDefault(g => g.Id == groupIds[i]);
            if (group != null)
            {
                group.Position = i;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(request.MenuId, cancellationToken))!;
    }

    public async Task<MenuDto> ReorderMenuGroupProductsAsync(ReorderMenuGroupProductsRequest request, CancellationToken cancellationToken = default)
    {
        var menuGroupExists = await _dbContext.MenuGroups.AnyAsync(g => g.Id == request.MenuGroupId && !g.IsDeleted, cancellationToken);
        if (!menuGroupExists)
            throw new KeyNotFoundException($"MenuGroup with ID {request.MenuGroupId} not found.");

        var productIds = request.ProductIdsInOrder.ToList();
        var products = await _dbContext.MenuGroupProducts
            .Where(gp => gp.MenuGroupId == request.MenuGroupId && productIds.Contains(gp.ProductId))
            .ToListAsync(cancellationToken);

        for (int i = 0; i < productIds.Count; i++)
        {
            var product = products.FirstOrDefault(p => p.ProductId == productIds[i]);
            if (product != null)
            {
                product.Position = i;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        var menuGroup = await _dbContext.MenuGroups.FirstAsync(g => g.Id == request.MenuGroupId, cancellationToken);
        return (await GetByIdAsync(menuGroup.MenuId, cancellationToken))!;
    }

    public async Task<MenuDto> ReorderMenuRootItemsAsync(ReorderMenuRootItemsRequest request, CancellationToken cancellationToken = default)
    {
        var menuExists = await _dbContext.Menus.AnyAsync(m => m.Id == request.MenuId && !m.IsDeleted, cancellationToken);
        if (!menuExists)
            throw new KeyNotFoundException($"Menu with ID {request.MenuId} not found.");

        var itemsInOrder = request.ItemsInOrder.ToList();

        // Update MenuProducts positions
        var productItems = itemsInOrder.Where(i => i.Type == RootItemType.Product).ToList();
        if (productItems.Any())
        {
            var productIds = productItems.Select(i => i.Id).ToList();
            var menuProducts = await _dbContext.MenuProducts
                .Where(mp => mp.MenuId == request.MenuId && productIds.Contains(mp.Id))
                .ToListAsync(cancellationToken);

            foreach (var item in productItems)
            {
                var menuProduct = menuProducts.FirstOrDefault(mp => mp.Id == item.Id);
                if (menuProduct != null)
                {
                    menuProduct.Position = item.Position;
                }
            }
        }

        // Update MenuGroups positions
        var groupItems = itemsInOrder.Where(i => i.Type == RootItemType.MenuGroup).ToList();
        if (groupItems.Any())
        {
            var groupIds = groupItems.Select(i => i.Id).ToList();
            var menuGroups = await _dbContext.MenuGroups
                .Where(g => g.MenuId == request.MenuId && groupIds.Contains(g.Id))
                .ToListAsync(cancellationToken);

            foreach (var item in groupItems)
            {
                var menuGroup = menuGroups.FirstOrDefault(g => g.Id == item.Id);
                if (menuGroup != null)
                {
                    menuGroup.Position = item.Position;
                }
            }
        }

        // Update MenuGroupReferences positions
        var groupRefItems = itemsInOrder.Where(i => i.Type == RootItemType.GroupReference).ToList();
        if (groupRefItems.Any())
        {
            var groupRefIds = groupRefItems.Select(i => i.Id).ToList();
            var groupRefs = await _dbContext.MenuGroupReferences
                .Where(gr => gr.MenuId == request.MenuId && groupRefIds.Contains(gr.Id))
                .ToListAsync(cancellationToken);

            foreach (var item in groupRefItems)
            {
                var groupRef = groupRefs.FirstOrDefault(gr => gr.Id == item.Id);
                if (groupRef != null)
                {
                    groupRef.Position = item.Position;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(request.MenuId, cancellationToken))!;
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

        var menuGroups = _dbContext.MenuGroupReferences.Where(mg => mg.GroupId == groupId);
        _dbContext.MenuGroupReferences.RemoveRange(menuGroups);

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

        var groups = menu.Groups.OrderBy(g => g.Position)
            .Select(g => new MenuGroupDto(
                g.Id,
                g.Name,
                g.Position,
                g.Products.Where(gp => gp.Product != null).OrderBy(gp => gp.Position)
                    .Select(gp => new MenuGroupProductDto(
                        gp.Id,
                        gp.ProductId,
                        gp.Product!.Name,
                        gp.Product.SalePrice,
                        gp.Position))));

        var groupReferences = menu.GroupReferences.Where(g => g.Group != null).OrderBy(g => g.Position)
            .Select(g => new MenuGroupReferenceDto(
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

        return new MenuDto(menu.Id, menu.Name, menu.StoreId, products, groups, groupReferences);
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