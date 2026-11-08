using Swaps.Bll.Dtos;

namespace Swaps.Bll.Caching;

/// <summary>
/// Reads a swap card without a participant check, so an adapter can tell "missing" from "not a participant".
/// Callers are responsible for the access check.
/// </summary>
public interface ISwapCardReader
{
    Task<SwapDto?> GetCardAsync(int swapId, CancellationToken ct = default);
}
