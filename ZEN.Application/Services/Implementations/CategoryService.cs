using AutoMapper;
using System.Linq.Expressions;
using ZEN.Application.DTOs.Products;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly IStoreContext _storeContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CategoryService(
        IRepository<Category> categoryRepository,
        IStoreContext storeContext,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _storeContext = storeContext;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(
            id,
            new Expression<Func<Category, object>>[] { c => c.ParentCategory! },
            cancellationToken);
        return category == null ? null : _mapper.Map<CategoryDto>(category);
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(
            new Expression<Func<Category, object>>[] { c => c.ParentCategory! },
            cancellationToken);
        return _mapper.Map<IEnumerable<CategoryDto>>(OrderHierarchically(categories));
    }

    public async Task<IEnumerable<CategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.FindAsync(
            c => c.IsActive,
            new Expression<Func<Category, object>>[] { c => c.ParentCategory! },
            cancellationToken);
        return _mapper.Map<IEnumerable<CategoryDto>>(OrderHierarchically(categories));
    }

    private static IEnumerable<Category> OrderHierarchically(IEnumerable<Category> categories)
    {
        var categoriesList = categories.ToList();
        var childrenByParent = categoriesList
            .Where(c => c.ParentCategoryId.HasValue)
            .GroupBy(c => c.ParentCategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.SortOrder).ToList());

        var result = new List<Category>();
        var roots = categoriesList.Where(c => !c.ParentCategoryId.HasValue).OrderBy(c => c.SortOrder).ToList();

        foreach (var root in roots)
        {
            AddCategoryAndChildren(root, childrenByParent, result);
        }

        return result;
    }

    private static void AddCategoryAndChildren(Category category, Dictionary<Guid, List<Category>> childrenByParent, List<Category> result)
    {
        result.Add(category);
        if (childrenByParent.TryGetValue(category.Id, out var children))
        {
            foreach (var child in children)
            {
                AddCategoryAndChildren(child, childrenByParent, result);
            }
        }
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = _mapper.Map<Category>(request);
        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> UpdateAsync(UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (category == null)
            throw new KeyNotFoundException("Category not found");

        _mapper.Map(request, category);
        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
            return false;

        _categoryRepository.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
