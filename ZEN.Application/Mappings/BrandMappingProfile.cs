using AutoMapper;
using System.Text;
using ZEN.Application.DTOs.Customers;
using ZEN.Application.DTOs.Products;
using ZEN.Domain.Entities;

namespace ZEN.Application.Mappings
{
    public class BrandMappingProfile : Profile
    {
        public BrandMappingProfile()
        {
            CreateMap<CreateBrandRequest, Brand>();

            CreateMap<UpdateBrandRequest, Brand>();

            CreateMap<Brand, BrandDto>();
        }
    }
}
