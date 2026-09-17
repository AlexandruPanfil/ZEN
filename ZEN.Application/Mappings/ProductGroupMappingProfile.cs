using AutoMapper;
using ZEN.Application.DTOs.Products;
using ZEN.Domain.Entities;

namespace ZEN.Application.Mappings;

public class ProductGroupMappingProfile : Profile
{
    public ProductGroupMappingProfile()
    {
        CreateMap<CreateProductGroupRequest, ProductGroup>();
        CreateMap<UpdateProductGroupRequest, ProductGroup>();
        CreateMap<ProductGroup, ProductGroupDto>()
            .ForMember(d => d.ParentGroupName, opt => opt.MapFrom(s => s.ParentGroup != null ? s.ParentGroup.Name : null));
    }
}