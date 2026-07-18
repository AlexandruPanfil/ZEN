using AutoMapper;
using ZEN.Application.DTOs.Inventory;
using ZEN.Domain.Entities;

namespace ZEN.Application.Mappings;

public class InventoryMappingProfile : Profile
{
    public InventoryMappingProfile()
    {
        CreateMap<InventoryItem, InventoryItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product.Name))
            .ForMember(d => d.SKU, opt => opt.MapFrom(s => s.Product.SKU))
            .ForMember(d => d.Barcode, opt => opt.MapFrom(s => s.Product.Barcode))
            .ForMember(d => d.WarehouseName, opt => opt.MapFrom(s => s.Warehouse.Name))
            .ForMember(d => d.ReorderLevel, opt => opt.MapFrom(s => s.MinQuantity));

        CreateMap<StockPurchase, StockPurchaseDto>()
            .ForMember(d => d.SupplierName, opt => opt.MapFrom(s => s.Supplier.Name))
            .ForMember(d => d.WarehouseName, opt => opt.MapFrom(s => s.Warehouse.Name))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));

        CreateMap<StockPurchaseItem, StockPurchaseItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product.Name));

        CreateMap<CreateStockPurchaseRequest, StockPurchase>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Number, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.Items, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<CreateStockPurchaseItemRequest, StockPurchaseItem>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.StockPurchaseId, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<StockTransfer, StockTransferDto>()
            .ForMember(d => d.SourceWarehouseName, opt => opt.MapFrom(s => s.SourceWarehouse.Name))
            .ForMember(d => d.DestinationWarehouseName, opt => opt.MapFrom(s => s.DestinationWarehouse.Name))
            .ForMember(d => d.StoreName, opt => opt.MapFrom(s => s.SourceWarehouse.Store.Name))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));

        CreateMap<StockTransferItem, StockTransferItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product.Name));
    }
}
