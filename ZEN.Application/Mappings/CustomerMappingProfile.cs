using AutoMapper;
using ZEN.Application.DTOs.Customers;
using ZEN.Domain.Entities;

namespace ZEN.Application.Mappings;

public class CustomerMappingProfile : Profile
{
    public CustomerMappingProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.CustomerGroupName, opt => opt.MapFrom(s => s.CustomerGroup != null ? s.CustomerGroup.Name : null));

        CreateMap<CreateCustomerRequest, Customer>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.LoyaltyPoints, opt => opt.Ignore())
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => true))
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateCustomerRequest, Customer>()
            .ForMember(d => d.LoyaltyPoints, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<CustomerGroup, CustomerGroupDto>();
    }
}
