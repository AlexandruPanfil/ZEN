namespace ZEN.Application.DTOs.CashDeskMenu;

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

public record MenuGroupDto(
    Guid Id,
    Guid GroupId,
    string GroupName,
    int Position,
    IEnumerable<GroupProductDto> Products);

public record MenuDto(
    Guid Id,
    string Name,
    Guid? StoreId,
    IEnumerable<MenuProductDto> Products,
    IEnumerable<MenuGroupDto> Groups);

public record MenuSummaryDto(
    Guid Id,
    string Name,
    Guid? StoreId);

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
    Guid? StoreId);

public record AssignMenuToCashDeskRequest(
    Guid CashDeskId,
    Guid? MenuId);

public record AddProductToMenuRequest(
    Guid MenuId,
    Guid ProductId,
    int Position);

public record AddGroupToMenuRequest(
    Guid MenuId,
    Guid GroupId,
    int Position);

public record AddProductToGroupRequest(
    Guid GroupId,
    Guid ProductId,
    int Position);

public record CreateGroupRequest(
    string Name,
    int DisplayOrder,
    Guid? MenuId);

public record UpdateGroupRequest(
    Guid Id,
    string Name,
    int DisplayOrder,
    Guid? MenuId);
