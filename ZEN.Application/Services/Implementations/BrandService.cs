using AutoMapper;
using ZEN.Application.DTOs.Products;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class BrandService : IBrandService
{
    private readonly IRepository<Brand> _brandRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BrandService(
        IRepository<Brand> brandRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _brandRepository = brandRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BrandDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepository.GetByIdAsync(id, cancellationToken);
        return brand == null ? null : _mapper.Map<BrandDto>(brand);
    }

    public async Task<IEnumerable<BrandDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var brands = await _brandRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<BrandDto>>(brands);
    }

    public async Task<IEnumerable<BrandDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var brands = await _brandRepository.FindAsync(b => b.IsActive, cancellationToken);
        return _mapper.Map<IEnumerable<BrandDto>>(brands);
    }

    public async Task<BrandDto> CreateAsync(CreateBrandRequest request, CancellationToken cancellationToken = default)
    {
        var brand = _mapper.Map<Brand>(request);
        await _brandRepository.AddAsync(brand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<BrandDto>(brand);
    }

    public async Task<BrandDto> UpdateAsync(UpdateBrandRequest request, CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepository.GetByIdAsync(request.Id, cancellationToken);
        if (brand == null)
            throw new KeyNotFoundException("Brand not found");

        _mapper.Map(request, brand);
        _brandRepository.Update(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<BrandDto>(brand);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _brandRepository.GetByIdAsync(id, cancellationToken);
        if (brand == null)
            return false;

        _brandRepository.Remove(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
