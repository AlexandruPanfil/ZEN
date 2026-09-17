using AutoMapper;
using ZEN.Application.DTOs.Products;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class ProductGroupService : IProductGroupService
{
    private readonly IRepository<ProductGroup> _productGroupRepository;
    private readonly IStoreContext _storeContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductGroupService(
        IRepository<ProductGroup> productGroupRepository,
        IStoreContext storeContext,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _productGroupRepository = productGroupRepository;
        _storeContext = storeContext;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProductGroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var productGroup = await _productGroupRepository.GetByIdAsync(id, cancellationToken);
        return productGroup == null ? null : _mapper.Map<ProductGroupDto>(productGroup);
    }

    public async Task<IEnumerable<ProductGroupDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var productGroups = await _productGroupRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ProductGroupDto>>(productGroups);
    }

    public async Task<IEnumerable<ProductGroupDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var productGroups = await _productGroupRepository.FindAsync(pg => pg.IsActive, cancellationToken);
        return _mapper.Map<IEnumerable<ProductGroupDto>>(productGroups.OrderBy(pg => pg.SortOrder));
    }

    public async Task<ProductGroupDto> CreateAsync(CreateProductGroupRequest request, CancellationToken cancellationToken = default)
    {
        var productGroup = _mapper.Map<ProductGroup>(request);
        await _productGroupRepository.AddAsync(productGroup, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProductGroupDto>(productGroup);
    }

    public async Task<ProductGroupDto> UpdateAsync(UpdateProductGroupRequest request, CancellationToken cancellationToken = default)
    {
        var productGroup = await _productGroupRepository.GetByIdAsync(request.Id, cancellationToken);
        if (productGroup == null)
            throw new KeyNotFoundException("ProductGroup not found");

        _mapper.Map(request, productGroup);
        _productGroupRepository.Update(productGroup);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProductGroupDto>(productGroup);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var productGroup = await _productGroupRepository.GetByIdAsync(id, cancellationToken);
        if (productGroup == null)
            return false;

        _productGroupRepository.Remove(productGroup);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}