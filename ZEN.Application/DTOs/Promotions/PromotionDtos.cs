namespace ZEN.Application.DTOs.Promotions;

public record DiscountDto(
    Guid Id,
    string Code,
    string? Description,
    string Type,
    decimal Value,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool IsActive,
    List<string> Conditions);

public record CreateDiscountRequest(
    string Code,
    string? Description,
    string Type,
    decimal Value,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool IsActive,
    List<string> Conditions);

public record UpdateDiscountRequest(
    Guid Id,
    string Code,
    string? Description,
    string Type,
    decimal Value,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool IsActive,
    List<string> Conditions);

public record PromotionDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status,
    List<Guid> DiscountIds,
    bool IsArchived);

public record CreatePromotionRequest(
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status,
    List<Guid> DiscountIds);

public record UpdatePromotionRequest(
    Guid Id,
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status,
    List<Guid> DiscountIds,
    bool IsArchived);
