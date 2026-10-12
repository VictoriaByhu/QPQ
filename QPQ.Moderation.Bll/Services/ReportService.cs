using System.Data;
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
}