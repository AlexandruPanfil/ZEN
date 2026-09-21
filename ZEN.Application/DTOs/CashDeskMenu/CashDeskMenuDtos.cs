namespace ZEN.Application.DTOs.CashDeskMenu;

public record MenuGroupProductDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal SalePrice,
    int Position);

public record MenuGroupDto(
    Guid Id,
    string Name,
    int Position,
    IEnumerable<MenuGroupProductDto> Products)
{
    public string GroupName => Name;
};

public record MenuGroupReferenceDto(
    Guid Id,
    Guid GroupId,
    string GroupName,
    int Position,
    IEnumerable<GroupProductDto> Products);

public record GroupProductDto(
    Guid ProductId,
    string ProductName,
    decimal SalePrice,
    int Position);

public record GroupDto(
    Guid Id,
    string Name,
    int DisplayOrder,
    Guid? MenuId,
    IEnumerable<GroupProductDto> Products);

public record MenuProductDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal SalePrice,
    int Position);

public record MenuDto(
    Guid Id,
    string Name,
    Guid? StoreId,
    IEnumerable<MenuProductDto> Products,
    IEnumerable<MenuGroupDto> Groups,
    IEnumerable<MenuGroupReferenceDto> GroupReferences);

public record MenuSummaryDto(
    Guid Id,
    string Name,
    Guid? StoreId,
    long Version = 0,
    bool IsDeleted = false);

public record FlatMenuItemDto(
    Guid ProductId,
    string Name,
    decimal SalePrice,
    Guid? GroupId,
    string? GroupName,
    decimal TaxRate);

public record CreateMenuRequest(
    string Name,
    Guid? StoreId,
    Guid? Id = null);

public record UpdateMenuRequest(
    Guid Id,
    string Name,
    Guid? StoreId,
    long BaseVersion = 0);

public record AssignMenuToCashDeskRequest(Guid CashDeskId, Guid? MenuId);

public record AddProductToMenuRequest(Guid MenuId, Guid ProductId, int Position);

public record AddGroupToMenuRequest(Guid MenuId, Guid GroupId, int Position);

public record AddProductToMenuGroupRequest(Guid MenuGroupId, Guid ProductId, int Position);

public record CreateMenuGroupRequest(string Name, int Position, Guid MenuId);

public record UpdateMenuGroupRequest(Guid Id, string Name, int Position);

public record ReorderMenuGroupsRequest(Guid MenuId, IEnumerable<Guid> GroupIdsInOrder);

public record ReorderMenuGroupProductsRequest(Guid MenuGroupId, IEnumerable<Guid> ProductIdsInOrder);

public record RemoveMenuProductRequest(Guid MenuProductId);

public record RemoveMenuGroupRequest(Guid MenuGroupId);

public record RemoveMenuGroupProductRequest(Guid MenuGroupProductId);

public record AddProductToGroupRequest(Guid GroupId, Guid ProductId, int Position);

public record CreateGroupRequest(string Name, int DisplayOrder, Guid? MenuId);

public record UpdateGroupRequest(Guid Id, string Name, int DisplayOrder, Guid? MenuId);

public enum AddProductAction
{
    Added,
    MovedFromRoot,
    MovedFromGroup,
    Skipped
}

public record AddProductResult(AddProductAction Action, Guid ProductId);