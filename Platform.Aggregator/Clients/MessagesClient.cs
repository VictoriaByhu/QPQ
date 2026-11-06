using System.Diagnostics;
using Platform.Aggregator.Dtos;

namespace Platform.Aggregator.Clients;

public sealed class MessagesClient(HttpClient http, ILogger<MessagesClient> logger)
{
    private const int PageSize = 100;

    public async Task<ConversationMessagesDto> GetMessagesForSwapAsync(int swapId, CancellationToken ct)
    {
        logger.LogInformation("Calling messages-api for swap {SwapId}", swapId);
        var sw = Stopwatch.StartNew();

        var conversation = await FindConversationAsync(swapId, ct);
        if (conversation is null)
        {
            logger.LogInformation("messages-api has no conversation for swap {SwapId}, responded in {Elapsed} ms", swapId, sw.ElapsedMilliseconds);
            return ConversationMessagesDto.Empty;
        }

        var messages = new List<MessageDto>();
        for (var page = 1; ; page++)
        {
            var result = await http.GetFromJsonAsync<PagedResult<MessageDto>>(
                $"api/conversations/{conversation.Id}/messages?page={page}&pageSize={PageSize}", ct);
            if (result is null)
                break;

            messages.AddRange(result.Items);
            if (page >= result.TotalPages)
                break;
        }

        logger.LogInformation("messages-api responded in {Elapsed} ms with {Count} messages for conversation {ConversationId}",
            sw.ElapsedMilliseconds, messages.Count, conversation.Id);
        return new ConversationMessagesDto(conversation, messages);
    }

    private async Task<ConversationDto?> FindConversationAsync(int swapId, CancellationToken ct)
    {
        for (var page = 1; ; page++)
        {
            var result = await http.GetFromJsonAsync<PagedResult<ConversationDto>>(
                $"api/conversations?page={page}&pageSize={PageSize}", ct);
            if (result is null)
                return null;

            var match = result.Items.FirstOrDefault(c => c.SwapId == swapId);
            if (match is not null || page >= result.TotalPages)
                return match;
        }
    }
}
