using System.Diagnostics;
using System.Net;
using Platform.Aggregator.Dtos;

namespace Platform.Aggregator.Clients;

public sealed class SwapsClient(HttpClient http, ILogger<SwapsClient> logger)
{
    public async Task<SwapDto?> GetSwapAsync(int swapId, CancellationToken ct)
    {
        logger.LogInformation("Calling swaps-api for swap {SwapId}", swapId);
        var sw = Stopwatch.StartNew();
        try
        {
            var swap = await http.GetFromJsonAsync<SwapDto>($"api/swaps/{swapId}", ct);
            logger.LogInformation("swaps-api responded in {Elapsed} ms", sw.ElapsedMilliseconds);
            return swap;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("swaps-api responded 404 for swap {SwapId} in {Elapsed} ms", swapId, sw.ElapsedMilliseconds);
            return null;
        }
    }
}
