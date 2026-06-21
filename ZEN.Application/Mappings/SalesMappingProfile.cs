using AutoMapper;
using ZEN.Application.DTOs.Sales;
using ZEN.Domain.Entities;

namespace ZEN.Application.Mappings;

public class SalesMappingProfile : Profile
{
    public SalesMappingProfile()
    {
        CreateMap<Sale, SaleDto>()
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer != null ? s.Customer.FullName : null))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items))
            .ForMember(d => d.Payments, opt => opt.MapFrom(s => s.Payments));

        CreateMap<SaleItem, SaleItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product.Name));

        CreateMap<SalePayment, SalePaymentDto>();

        CreateMap<CreateSaleRequest, Sale>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Number, opt => opt.Ignore())
            .ForMember(d => d.Date, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.Items, opt => opt.Ignore())
            .ForMember(d => d.Payments, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<CreateSaleItemRequest, SaleItem>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.SaleId, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<CreateSalePaymentRequest, SalePayment>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.SaleId, opt => opt.Ignore())
            .ForMember(d => d.PaidAt, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<HeldSale, HeldSaleDto>()
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer != null ? s.Customer.FullName : null));

        CreateMap<HoldSaleRequest, HeldSale>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.UserId, opt => opt.Ignore())
            .ForMember(d => d.HeldAt, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());
    }
}
