using PromotionDto = ZEN.Application.Services.Interfaces.PromotionDto;
using CreatePromotionRequest = ZEN.Application.Services.Interfaces.CreatePromotionRequest;
using UpdatePromotionRequest = ZEN.Application.Services.Interfaces.UpdatePromotionRequest;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class PromotionEndpoints
{
    public static void MapPromotionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllPromotions)
            .WithName("GetAllPromotions")
            .WithOpenApi()
            .Produces<IEnumerable<PromotionDto>>(200);

        group.MapGet("/active", GetActivePromotions)
            .WithName("GetActivePromotions")
            .WithOpenApi()
            .Produces<IEnumerable<PromotionDto>>(200);

        group.MapGet("/store/{storeId:guid}", GetPromotionsByStore)
            .WithName("GetPromotionsByStore")
            .WithOpenApi()
            .Produces<IEnumerable<PromotionDto>>(200);

        group.MapGet("/store/{storeId:guid}/active", GetActivePromotionsByStore)
            .WithName("GetActivePromotionsByStore")
            .WithOpenApi()
            .Produces<IEnumerable<PromotionDto>>(200);

        group.MapGet("/{id:guid}", GetPromotionById)
            .WithName("GetPromotionById")
            .WithOpenApi()
            .Produces<PromotionDto>(200)
            .Produces(404);

        group.MapPost("/", CreatePromotion)
            .WithName("CreatePromotion")
            .WithOpenApi()
            .Produces<PromotionDto>(201)
            .Produces(400);

        group.MapPut("/{id:guid}", UpdatePromotion)
            .WithName("UpdatePromotion")
            .WithOpenApi()
            .Produces<PromotionDto>(200)
            .Produces(404);

        group.MapDelete("/{id:guid}", DeletePromotion)
            .WithName("DeletePromotion")
            .WithOpenApi()
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> GetAllPromotions(IPromotionService service, CancellationToken ct)
    {
        var promotions = await service.GetAllAsync(ct);
        return Results.Ok(promotions);
    }

    private static async Task<IResult> GetActivePromotions(IPromotionService service, CancellationToken ct)
    {
        var promotions = await service.GetActiveAsync(ct);
        return Results.Ok(promotions);
    }

    private static async Task<IResult> GetPromotionsByStore(Guid storeId, IPromotionService service, CancellationToken ct)
    {
        var promotions = await service.GetByStoreAsync(storeId, ct);
        return Results.Ok(promotions);
    }

    private static async Task<IResult> GetActivePromotionsByStore(Guid storeId, IPromotionService service, CancellationToken ct)
    {
        var promotions = await service.GetActiveByStoreAsync(storeId, ct);
        return Results.Ok(promotions);
    }

    private static async Task<IResult> GetPromotionById(Guid id, IPromotionService service, CancellationToken ct)
    {
        var promotion = await service.GetByIdAsync(id, ct);
        return promotion == null ? Results.NotFound() : Results.Ok(promotion);
    }

    private static async Task<IResult> CreatePromotion(CreatePromotionRequest request, IPromotionService service, CancellationToken ct)
    {
        var promotion = await service.CreateAsync(request, ct);
        return Results.Created($"/api/promotions/{promotion.Id}", promotion);
    }

    private static async Task<IResult> UpdatePromotion(Guid id, UpdatePromotionRequest request, IPromotionService service, CancellationToken ct)
    {
        if (id != request.Id)
            return Results.BadRequest("Id mismatch");

        var promotion = await service.UpdateAsync(request, ct);
        return Results.Ok(promotion);
    }

    private static async Task<IResult> DeletePromotion(Guid id, IPromotionService service, CancellationToken ct)
    {
        var success = await service.DeleteAsync(id, ct);
        return success ? Results.NoContent() : Results.NotFound();
    }
}
