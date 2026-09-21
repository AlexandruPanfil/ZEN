using ZEN.Application.DTOs.CashDeskMenu;

namespace ZEN.Application.Services.Interfaces;

public interface IMenuService
{
    Task<IEnumerable<MenuSummaryDto>> GetAllSummariesAsync(
        CancellationToken cancellationToken = default,
        bool includeDeleted = false);
    Task<MenuDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MenuDto> CreateAsync(CreateMenuRequest request, CancellationToken cancellationToken = default);
    Task<MenuDto> UpdateAsync(UpdateMenuRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default, long baseVersion = 0);
    Task<IEnumerable<FlatMenuItemDto>> GetFlatItemsAsync(Guid menuId, CancellationToken cancellationToken = default);
    Task<AddProductResult> AddProductAsync(AddProductToMenuRequest request, CancellationToken cancellationToken = default);
    Task<MenuDto> AddGroupAsync(AddGroupToMenuRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveMenuProductAsync(Guid menuProductId, CancellationToken cancellationToken = default);
    Task<bool> RemoveMenuGroupReferenceAsync(Guid menuGroupReferenceId, CancellationToken cancellationToken = default);

    Task<MenuDto> AddMenuGroupAsync(CreateMenuGroupRequest request, CancellationToken cancellationToken = default);
    Task<MenuDto> UpdateMenuGroupAsync(UpdateMenuGroupRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveMenuGroupAsync(Guid menuGroupId, CancellationToken cancellationToken = default);
    Task<AddProductResult> AddProductToMenuGroupAsync(AddProductToMenuGroupRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveProductFromMenuGroupAsync(Guid menuGroupProductId, CancellationToken cancellationToken = default);
    Task<MenuDto> ReorderMenuGroupsAsync(ReorderMenuGroupsRequest request, CancellationToken cancellationToken = default);
    Task<MenuDto> ReorderMenuGroupProductsAsync(ReorderMenuGroupProductsRequest request, CancellationToken cancellationToken = default);
    Task<MenuDto> ReorderMenuRootItemsAsync(ReorderMenuRootItemsRequest request, CancellationToken cancellationToken = default);

    Task<IEnumerable<GroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default);
    Task<GroupDto> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);
    Task<GroupDto> UpdateGroupAsync(UpdateGroupRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<bool> AddProductToGroupAsync(AddProductToGroupRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveProductFromGroupAsync(Guid groupProductId, CancellationToken cancellationToken = default);
}