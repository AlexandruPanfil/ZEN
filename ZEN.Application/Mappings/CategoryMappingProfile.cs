using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using ZEN.Application.DTOs.Customers;
using ZEN.Application.DTOs.Products;
using ZEN.Domain.Entities;

namespace ZEN.Application.Mappings
{
    public class CategoryMappingProfile : Profile
    {
        public CategoryMappingProfile()
        {
            CreateMap<CreateCategoryRequest, Category>()
                .ForMember(d => d.ImagePath, opt => opt.Ignore());

            CreateMap<UpdateCategoryRequest, Category>()
                .ForMember(d => d.ImagePath, opt => opt.Ignore());

            CreateMap<Category, CategoryDto>();
        }
    }

}
