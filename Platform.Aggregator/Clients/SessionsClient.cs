using System.Diagnostics;
using System.Net;
using Platform.Aggregator.Dtos;

namespace Platform.Aggregator.Clients;

public sealed class SessionsClient(HttpClient http, ILogger<SessionsClient> logger)
{
    public async Task<IReadOnlyList<SessionDto>> GetSessionsForSwapAsync(int swapId, CancellationToken ct)
    {
        logger.LogInformation("Calling sessions-api for swap {SwapId}", swapId);
        var sw = Stopwatch.StartNew();
        try
        {
            var accepted = await http.GetFromJsonAsync<AcceptedSwapSessionsDto>($"api/accepted-swaps/{swapId}/sessions", ct);
            logger.LogInformation("sessions-api responded in {Elapsed} ms", sw.ElapsedMilliseconds);
            return accepted?.Sessions ?? [];
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("sessions-api has no accepted swap {SwapId}, responded in {Elapsed} ms", swapId, sw.ElapsedMilliseconds);
            return [];
        }
    }
}
