/*using System.Data;
using AutoMapper;
using Microsoft.Data.SqlClient; // Додано для перехоплення SqlException
using QPQ.Moderation.Bll.DTOs;
using QPQ.Moderation.Bll.Interfaces;
using QPQ.Moderation.Dal.Interfaces;
using QPQ.Moderation.Dal;
using QPQ.Moderation.Domain.Exceptions;

namespace QPQ.Moderation.Bll.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ReportService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ReportDto> GetReportAsync(long id, CancellationToken ct = default)
    {
        var report = await _uow.Reports.GetByIdAsync(id, ct);
        if (report == null)
            throw new NotFoundException($"Скарга з ID {id} не знайдена.");

        return _mapper.Map<ReportDto>(report);
    }

    public async Task ChangeReportStatusAsync(long id, string newStatus, CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var report = await _uow.Reports.GetByIdAsync(id, ct);
            if (report == null)
                throw new NotFoundException($"Скарга з ID {id} не знайдена.");

            await _uow.Reports.ChangeStatusAsync(id, newStatus, ct);
            await _uow.CommitAsync(ct);
        }
        catch (SqlException ex) when (ex.Number == 50002) // Перехоплюємо бізнес-помилку з бази
        {
            await _uow.RollbackAsync(ct);
            throw new BusinessConflictException(ex.Message);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}*/
using AutoMapper;
using QPQ.Moderation.Bll.DTOs;
using QPQ.Moderation.Bll.Interfaces;
using QPQ.Moderation.Dal.Interfaces;
using QPQ.Moderation.Domain;
using QPQ.Moderation.Domain.Exceptions;
using QPQ.Moderation.Domain.Specifications;

namespace QPQ.Moderation.Bll.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReportService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ReportDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var report = await _unitOfWork.Reports.GetByIdAsync(id);
        return report == null ? null : _mapper.Map<ReportDto>(report);
    }

    public async Task<ReportDto> CreateAsync(CreateReportDto dto, CancellationToken ct = default)
    {
        // 1. Конвертуємо DTO в сутність БД за допомогою AutoMapper
        var report = _mapper.Map<Report>(dto);

        // 2. Додаємо в репозиторій
        await _unitOfWork.Reports.AddAsync(report);

        // 3. Зберігаємо зміни в БД однією транзакцією
        await _unitOfWork.SaveChangesAsync(ct);

        // 4. Повертаємо клієнту безпечний DTO
        return _mapper.Map<ReportDto>(report);
    }
    public async Task<PagedResult<ReportDto>> GetPagedAsync(string? status, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var spec = new ReportsByStatusSpecification(status); // Або як у тебе називається специфікація

        // 1. Отримуємо кортеж (дані та загальну кількість) з репозиторію
        var (items, totalCount) = await _unitOfWork.Reports.GetPagedAsync(spec, pageNumber, pageSize, ct);

        // 2. Мапимо сутності в DTO
        var dtos = _mapper.Map<List<ReportDto>>(items);

        // 3. Повертаємо готовий об'єкт із метаданими пагінації
        return new PagedResult<ReportDto>(dtos, totalCount, pageNumber, pageSize);
    }
    public async Task<ReportDto> GetWithExplicitLoadingAsync(long id, CancellationToken ct = default)
    {
        // Викликаємо наш новий метод з репозиторію
        var report = await _unitOfWork.Reports.GetWithExplicitLoadingAsync(id, ct);

        if (report == null)
            throw new NotFoundException($"Скарга з ID {id} не знайдена."); // Або використай BusinessConflictException, якщо ти залишив його

        return _mapper.Map<ReportDto>(report);
    }
}