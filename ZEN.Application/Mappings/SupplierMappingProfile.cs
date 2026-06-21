using AutoMapper;
using ZEN.Application.DTOs.Suppliers;
using ZEN.Domain.Entities;

namespace ZEN.Application.Mappings;

public class SupplierMappingProfile : Profile
{
    public SupplierMappingProfile()
    {
        CreateMap<Supplier, SupplierDto>();

        CreateMap<CreateSupplierRequest, Supplier>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.TotalPurchases, opt => opt.Ignore())
            .ForMember(d => d.TotalPaid, opt => opt.Ignore())
            .ForMember(d => d.Balance, opt => opt.Ignore())
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => true))
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateSupplierRequest, Supplier>()
            .ForMember(d => d.TotalPurchases, opt => opt.Ignore())
            .ForMember(d => d.TotalPaid, opt => opt.Ignore())
            .ForMember(d => d.Balance, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());
    }
}
