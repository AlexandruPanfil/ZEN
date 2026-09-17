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

            CreateMap<Category, CategoryDto>()
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Description))
                .ForMember(d => d.SortOrder, opt => opt.MapFrom(s => s.SortOrder))
                .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.IsActive));
        }
    }
}