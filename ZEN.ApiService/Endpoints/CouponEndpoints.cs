using ZEN.Application.DTOs.Promotions;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class CouponEndpoints
{
    public static void MapCouponEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllCoupons)
            .WithName("GetAllCoupons")
            .WithOpenApi()
            .Produces<IEnumerable<CouponDto>>(200);

        group.MapGet("/active", GetActiveCoupons)
            .WithName("GetActiveCoupons")
            .WithOpenApi()
            .Produces<IEnumerable<CouponDto>>(200);

        group.MapGet("/{id:guid}", GetCouponById)
            .WithName("GetCouponById")
            .WithOpenApi()
            .Produces<CouponDto>(200)
            .Produces(404);

        group.MapGet("/code/{code}", GetCouponByCode)
            .WithName("GetCouponByCode")
            .WithOpenApi()
            .Produces<CouponDto>(200)
            .Produces(404);

        group.MapPost("/", CreateCoupon)
            .WithName("CreateCoupon")
            .WithOpenApi()
            .Produces<CouponDto>(201)
            .Produces(400);

        group.MapPut("/{id:guid}", UpdateCoupon)
            .WithName("UpdateCoupon")
            .WithOpenApi()
            .Produces<CouponDto>(200)
            .Produces(404);

        group.MapDelete("/{id:guid}", DeleteCoupon)
            .WithName("DeleteCoupon")
            .WithOpenApi()
            .Produces(204)
            .Produces(404);

        group.MapPost("/{code}/validate", ValidateCoupon)
            .WithName("ValidateCoupon")
            .WithOpenApi()
            .Produces<decimal>(200)
            .Produces(404);

        group.MapPost("/{id:guid}/redeem", RedeemCoupon)
            .WithName("RedeemCoupon")
            .WithOpenApi()
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> GetAllCoupons(ICouponService service, CancellationToken ct)
    {
        var coupons = await service.GetAllAsync(ct);
        return Results.Ok(coupons);
    }

    private static async Task<IResult> GetActiveCoupons(ICouponService service, CancellationToken ct)
    {
        var coupons = await service.GetActiveAsync(ct);
        return Results.Ok(coupons);
    }

    private static async Task<IResult> GetCouponById(Guid id, ICouponService service, CancellationToken ct)
    {
        var coupon = await service.GetByIdAsync(id, ct);
        return coupon == null ? Results.NotFound() : Results.Ok(coupon);
    }

    private static async Task<IResult> GetCouponByCode(string code, ICouponService service, CancellationToken ct)
    {
        var coupon = await service.GetByCodeAsync(code, ct);
        return coupon == null ? Results.NotFound() : Results.Ok(coupon);
    }

    private static async Task<IResult> CreateCoupon(CreateCouponRequest request, ICouponService service, CancellationToken ct)
    {
        var coupon = await service.CreateAsync(request, ct);
        return Results.Created($"/api/coupons/{coupon.Id}", coupon);
    }

    private static async Task<IResult> UpdateCoupon(Guid id, UpdateCouponRequest request, ICouponService service, CancellationToken ct)
    {
        if (id != request.Id)
            return Results.BadRequest("Id mismatch");

        var coupon = await service.UpdateAsync(request, ct);
        return Results.Ok(coupon);
    }

    private static async Task<IResult> DeleteCoupon(Guid id, ICouponService service, CancellationToken ct)
    {
        var success = await service.DeleteAsync(id, ct);
        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> ValidateCoupon(string code, decimal amount, ICouponService service, CancellationToken ct)
    {
        try
        {
            var discount = await service.ValidateAndApplyCouponAsync(code, amount, ct);
            return Results.Ok(discount);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> RedeemCoupon(Guid id, ICouponService service, CancellationToken ct)
    {
        try
        {
            var success = await service.RedeemCouponAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}
