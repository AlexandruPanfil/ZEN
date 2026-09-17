using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ZEN.Application.Services.Implementations;
using ZEN.Application.Services.Interfaces;

namespace ZEN.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Application Services
        services.AddScoped<IStoreContext, StoreContextService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductGroupService, ProductGroupService>();
        services.AddScoped<ITaxService, TaxService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IStockTransferService, StockTransferService>();
        services.AddScoped<IInventoryCountService, InventoryCountService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<ISellOrderService, SellOrderService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISyncService, SyncService>();

        return services;
    }
}