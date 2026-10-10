using System.Data;
using AutoMapper;
using Swaps.Bll.Dtos;
using Swaps.Dal.Interfaces;
using Swaps.Domain.Constants;
using Swaps.Domain.Exceptions;
using Swaps.Domain.Models;

namespace Swaps.Bll.Services;

public sealed class SwapService(IUnitOfWork uow, IMapper mapper) : ISwapService
{
    public async Task<SwapDto> CreateAsync(Guid userId, CreateSwapDto input, CancellationToken ct = default)
    {
        if (input.PartnerId == Guid.Empty)
            throw new ValidationException("PartnerId must not be empty.");
        if (input.PartnerId == userId)
            throw new ValidationException("A swap cannot be created with yourself.");
        if (input.OfferedSkillId == input.RequestedSkillId)
            throw new ValidationException("Offered and requested skills must be different.");

        int swapId;
        await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            swapId = await uow.Swaps.AddAsync(new Swap
            {
                InitiatorId = userId,
                PartnerId = input.PartnerId,
                Status = SwapStatuses.Pending,
                CreatedBy = userId
            }, ct);

            await uow.Swaps.AddDetailsAsync(new SwapDetails
            {
                SwapId = swapId,
                Message = input.Message,
                Location = input.Location,
                ProposedDate = input.ProposedDate,
                DurationMinutes = input.DurationMinutes
            }, ct);

            int[] requested = [input.OfferedSkillId, input.RequestedSkillId];
            var found = await uow.Skills.GetByIdsAsync(requested, ct);
            var missing = requested.Except(found.Select(s => s.Id)).ToList();
            if (missing.Count > 0)
                throw new NotFoundException($"Skill(s) not found: {string.Join(", ", missing)}");

            await uow.Swaps.AddSkillAsync(new SwapSkill
            {
                SwapId = swapId,
                SkillId = input.OfferedSkillId,
                Role = SwapSkillRoles.Offered
            }, ct);
            await uow.Swaps.AddSkillAsync(new SwapSkill
            {
                SwapId = swapId,
                SkillId = input.RequestedSkillId,
                Role = SwapSkillRoles.Requested
            }, ct);

            await uow.History.AddAsync(new SwapStatusHistoryEntry
            {
                SwapId = swapId,
                OldStatus = null,
                NewStatus = SwapStatuses.Pending,
                ChangedByUserId = userId,
                Comment = "Swap created"
            }, ct);

            await uow.CommitAsync(ct);
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }

        return await GetByIdAsync(userId, swapId, ct);
    }

    public async Task<SwapDto> GetByIdAsync(Guid userId, int swapId, CancellationToken ct = default)
    {
        var swap = await uow.Swaps.GetWithDetailsAsync(swapId, ct);
        if (swap is null || !IsParticipant(swap, userId))
            throw new NotFoundException($"Swap {swapId} not found");

        return mapper.Map<SwapDto>(swap);
    }

    public async Task<IReadOnlyList<SwapListItemDto>> GetMineAsync(
        Guid userId, SwapQueryDto query, CancellationToken ct = default)
    {
        if (query.Role is not null && query.Role is not (SwapRoles.Initiator or SwapRoles.Partner))
            throw new ValidationException($"Role must be '{SwapRoles.Initiator}' or '{SwapRoles.Partner}'.");
        if (query.Status is not null && !SwapStatuses.All.Contains(query.Status))
            throw new ValidationException($"Status must be one of: {string.Join(", ", SwapStatuses.All)}.");

        var swaps = await uow.Swaps.GetForUserAsync(userId, query.Role, query.Status, query.Page, query.PageSize, ct);
        return mapper.Map<List<SwapListItemDto>>(swaps);
    }

    public async Task<SwapDto> UpdateDetailsAsync(
        Guid userId, int swapId, UpdateSwapDetailsDto input, CancellationToken ct = default)
    {
        var swap = await LoadParticipantSwapAsync(userId, swapId, ct);
        if (swap.InitiatorId != userId)
            throw new BusinessConflictException("Only the initiator can edit swap details.");
        if (swap.Status != SwapStatuses.Pending)
            throw new BusinessConflictException("Details can be edited only while the swap is Pending.");

        var rowVersion = ParseRowVersion(input.RowVersion) ?? swap.RowVersion;

        await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            if (!await uow.Swaps.TouchAsync(swapId, userId, rowVersion, ct))
                throw new BusinessConflictException("The swap was modified by someone else. Reload it and try again.");

            var updated = await uow.Swaps.UpdateDetailsAsync(new SwapDetails
            {
                SwapId = swapId,
                Message = input.Message,
                Location = input.Location,
                ProposedDate = input.ProposedDate,
                DurationMinutes = input.DurationMinutes
            }, ct);
            if (!updated)
                throw new NotFoundException($"Details of swap {swapId} not found");

            await uow.CommitAsync(ct);
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }

        return await GetByIdAsync(userId, swapId, ct);
    }

    public async Task DeleteAsync(Guid userId, int swapId, CancellationToken ct = default)
    {
        var swap = await LoadParticipantSwapAsync(userId, swapId, ct);
        if (swap.InitiatorId != userId)
            throw new BusinessConflictException("Only the initiator can delete a swap.");
        if (swap.Status == SwapStatuses.Accepted)
            throw new BusinessConflictException("An accepted swap cannot be deleted. Cancel or complete it first.");

        await uow.Swaps.SoftDeleteAsync(swapId, userId, swap.RowVersion, ct);
    }

    public Task AcceptAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
        => ChangeStatusAsync(userId, swapId, SwapStatuses.Accepted, input, ct);

    public Task RejectAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
        => ChangeStatusAsync(userId, swapId, SwapStatuses.Rejected, input, ct);

    public Task CancelAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
        => ChangeStatusAsync(userId, swapId, SwapStatuses.Cancelled, input, ct);

    public Task CompleteAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
        => ChangeStatusAsync(userId, swapId, SwapStatuses.Completed, input, ct);

    public async Task<IReadOnlyList<SwapStatusHistoryDto>> GetHistoryAsync(
        Guid userId, int swapId, CancellationToken ct = default)
    {
        await LoadParticipantSwapAsync(userId, swapId, ct);
        var history = await uow.History.GetBySwapAsync(swapId, ct);
        return mapper.Map<List<SwapStatusHistoryDto>>(history);
    }

    private async Task ChangeStatusAsync(
        Guid userId, int swapId, string newStatus, ChangeSwapStatusDto? input, CancellationToken ct)
    {
        var swap = await LoadParticipantSwapAsync(userId, swapId, ct);
        var rowVersion = ParseRowVersion(input?.RowVersion) ?? swap.RowVersion;
        await uow.Swaps.ChangeStatusAsync(swapId, newStatus, userId, rowVersion, input?.Comment, ct);
    }

    private async Task<Swap> LoadParticipantSwapAsync(Guid userId, int swapId, CancellationToken ct)
    {
        var swap = await uow.Swaps.GetByIdAsync(swapId, ct);
        if (swap is null || !IsParticipant(swap, userId))
            throw new NotFoundException($"Swap {swapId} not found");

        return swap;
    }

    private static bool IsParticipant(Swap swap, Guid userId)
        => swap.InitiatorId == userId || swap.PartnerId == userId;

    private static byte[]? ParseRowVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(value);
            if (bytes.Length == 8)
                return bytes;
        }
        catch (FormatException)
        {
        }

        throw new ValidationException("RowVersion has an invalid format.");
    }
}
