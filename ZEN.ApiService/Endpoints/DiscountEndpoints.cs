using DiscountDto = ZEN.Application.Services.Interfaces.DiscountDto;
using CreateDiscountRequest = ZEN.Application.Services.Interfaces.CreateDiscountRequest;
using UpdateDiscountRequest = ZEN.Application.Services.Interfaces.UpdateDiscountRequest;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class DiscountEndpoints
{
    public static void MapDiscountEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllDiscounts)
            .WithName("GetAllDiscounts")
            .WithOpenApi()
            .Produces<IEnumerable<DiscountDto>>(200);

        group.MapGet("/active", GetActiveDiscounts)
            .WithName("GetActiveDiscounts")
            .WithOpenApi()
            .Produces<IEnumerable<DiscountDto>>(200);

        group.MapGet("/store/{storeId:guid}", GetDiscountsByStore)
            .WithName("GetDiscountsByStore")
            .WithOpenApi()
            .Produces<IEnumerable<DiscountDto>>(200);

        group.MapGet("/store/{storeId:guid}/active", GetActiveDiscountsByStore)
            .WithName("GetActiveDiscountsByStore")
            .WithOpenApi()
            .Produces<IEnumerable<DiscountDto>>(200);

        group.MapGet("/{id:guid}", GetDiscountById)
            .WithName("GetDiscountById")
            .WithOpenApi()
            .Produces<DiscountDto>(200)
            .Produces(404);

        group.MapGet("/code/{code}", GetDiscountByCode)
            .WithName("GetDiscountByCode")
            .WithOpenApi()
            .Produces<DiscountDto>(200)
            .Produces(404);

        group.MapPost("/", CreateDiscount)
            .WithName("CreateDiscount")
            .WithOpenApi()
            .Produces<DiscountDto>(201)
            .Produces(400);

        group.MapPut("/{id:guid}", UpdateDiscount)
            .WithName("UpdateDiscount")
            .WithOpenApi()
            .Produces<DiscountDto>(200)
            .Produces(404);

        group.MapDelete("/{id:guid}", DeleteDiscount)
            .WithName("DeleteDiscount")
            .WithOpenApi()
            .Produces(204)
            .Produces(404);

        group.MapPost("/{id:guid}/calculate", CalculateDiscount)
            .WithName("CalculateDiscount")
            .WithOpenApi()
            .Produces<decimal>(200)
            .Produces(404);
    }

    private static async Task<IResult> GetAllDiscounts(IDiscountService service, CancellationToken ct)
    {
        var discounts = await service.GetAllAsync(ct);
        return Results.Ok(discounts);
    }

    private static async Task<IResult> GetActiveDiscounts(IDiscountService service, CancellationToken ct)
    {
        var discounts = await service.GetActiveAsync(ct);
        return Results.Ok(discounts);
    }

    private static async Task<IResult> GetDiscountsByStore(Guid storeId, IDiscountService service, CancellationToken ct)
    {
        var discounts = await service.GetByStoreAsync(storeId, ct);
        return Results.Ok(discounts);
    }

    private static async Task<IResult> GetActiveDiscountsByStore(Guid storeId, IDiscountService service, CancellationToken ct)
    {
        var discounts = await service.GetActiveByStoreAsync(storeId, ct);
        return Results.Ok(discounts);
    }

    private static async Task<IResult> GetDiscountById(Guid id, IDiscountService service, CancellationToken ct)
    {
        var discount = await service.GetByIdAsync(id, ct);
        return discount == null ? Results.NotFound() : Results.Ok(discount);
    }

    private static async Task<IResult> GetDiscountByCode(string code, IDiscountService service, CancellationToken ct)
    {
        var discount = await service.GetByCodeAsync(code, ct);
        return discount == null ? Results.NotFound() : Results.Ok(discount);
    }

    private static async Task<IResult> CreateDiscount(CreateDiscountRequest request, IDiscountService service, CancellationToken ct)
    {
        var discount = await service.CreateAsync(request, ct);
        return Results.Created($"/api/discounts/{discount.Id}", discount);
    }

    private static async Task<IResult> UpdateDiscount(Guid id, UpdateDiscountRequest request, IDiscountService service, CancellationToken ct)
    {
        if (id != request.Id)
            return Results.BadRequest("Id mismatch");

        var discount = await service.UpdateAsync(request, ct);
        return Results.Ok(discount);
    }

    private static async Task<IResult> DeleteDiscount(Guid id, IDiscountService service, CancellationToken ct)
    {
        var success = await service.DeleteAsync(id, ct);
        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> CalculateDiscount(Guid id, decimal amount, IDiscountService service, CancellationToken ct)
    {
        var discount = await service.GetByIdAsync(id, ct);
        if (discount == null)
            return Results.NotFound();

        var discountAmount = await service.CalculateDiscountAsync(id, amount, ct);
        return Results.Ok(discountAmount);
    }
}
