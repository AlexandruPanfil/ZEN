using AutoMapper;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class TaxService : ITaxService
{
    private readonly IRepository<Tax> _taxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TaxService(
        IRepository<Tax> taxRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _taxRepository = taxRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TaxDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tax = await _taxRepository.GetByIdAsync(id, cancellationToken);
        return tax == null ? null : MapToDto(tax);
    }

    public async Task<IEnumerable<TaxDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var taxes = await _taxRepository.GetAllAsync(cancellationToken);
        return taxes.Select(MapToDto);
    }

    public async Task<IEnumerable<TaxDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var taxes = await _taxRepository.FindAsync(t => t.IsActive, cancellationToken);
        return taxes.Select(MapToDto);
    }

    public async Task<TaxDto?> GetDefaultTaxAsync(CancellationToken cancellationToken = default)
    {
        var defaultTax = await _taxRepository.FindFirstAsync(t => t.IsDefault, cancellationToken);
        return defaultTax == null ? null : MapToDto(defaultTax);
    }

    public async Task<TaxDto> CreateAsync(CreateTaxRequest request, CancellationToken cancellationToken = default)
    {
        var tax = new Tax
        {
            Name = request.Name,
            Rate = request.Rate,
            IsActive = request.IsActive,
            IsDefault = false
        };

        await _taxRepository.AddAsync(tax, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(tax);
    }

    public async Task<TaxDto> UpdateAsync(UpdateTaxRequest request, CancellationToken cancellationToken = default)
    {
        var tax = await _taxRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tax == null)
            throw new KeyNotFoundException("Tax not found");

        tax.Name = request.Name;
        tax.Rate = request.Rate;
        tax.IsDefault = request.IsDefault;
        tax.IsActive = request.IsActive;

        if (request.IsDefault)
        {
            var otherTaxes = await _taxRepository.FindAsync(t => t.Id != request.Id && t.IsDefault, cancellationToken);
            foreach (var other in otherTaxes)
            {
                other.IsDefault = false;
                _taxRepository.Update(other);
            }
        }

        _taxRepository.Update(tax);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(tax);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tax = await _taxRepository.GetByIdAsync(id, cancellationToken);
        if (tax == null)
            return false;

        _taxRepository.Remove(tax);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetDefaultTaxAsync(Guid taxId, CancellationToken cancellationToken = default)
    {
        var tax = await _taxRepository.GetByIdAsync(taxId, cancellationToken);
        if (tax == null)
            return false;

        var currentDefault = await _taxRepository.FindFirstAsync(t => t.IsDefault && t.Id != taxId, cancellationToken);
        if (currentDefault != null)
        {
            currentDefault.IsDefault = false;
            _taxRepository.Update(currentDefault);
        }

        tax.IsDefault = true;
        _taxRepository.Update(tax);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private TaxDto MapToDto(Tax tax) => new TaxDto(tax.Id, tax.Name, tax.Rate, tax.IsDefault, tax.IsActive);
}
