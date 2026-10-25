using MediatR;
using Microsoft.AspNetCore.Mvc;
using Messages.Application.Common.Models;
using Messages.Application.Conversations.Commands.ChangeSwapStatus;
using Messages.Application.Conversations.Commands.CreateConversation;
using Messages.Application.Conversations.Commands.DeleteConversation;
using Messages.Application.Conversations.Commands.MarkConversationRead;
using Messages.Application.Conversations.Dtos;
using Messages.Application.Conversations.Queries.GetConversationById;
using Messages.Application.Conversations.Queries.GetConversationsList;
using Messages.WebApi.Contracts;
using Messages.WebApi.Services;

namespace Messages.WebApi.Controllers;

[Route("api/conversations")]
public sealed class ConversationsController(ISender sender, ICurrentUser currentUser)
    : ApiControllerBase(sender, currentUser)
{
    [HttpGet]
    [EndpointSummary("Lists conversations of the current user, newest activity first.")]
    [ProducesResponseType<PagedResult<ConversationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ConversationDto>>> GetList(
        [FromQuery] string? swapStatus, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await Sender.Send(new GetConversationsListQuery(UserId, swapStatus, page, pageSize), ct));

    [HttpGet("{id}")]
    [EndpointSummary("Returns a conversation; the ETag header carries the document version.")]
    [ProducesResponseType<ConversationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationDto>> GetById(string id, CancellationToken ct)
    {
        var conversation = await Sender.Send(new GetConversationByIdQuery(id, UserId), ct);
        SetETag(conversation.Version);
        return Ok(conversation);
    }

    [HttpPost]
    [EndpointSummary("Opens a conversation for an accepted swap and posts the SwapAccepted system message.")]
    [ProducesResponseType<ConversationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConversationDto>> Create(CreateConversationRequest request, CancellationToken ct)
    {
        var conversation = await Sender.Send(new CreateConversationCommand(
            UserId, request.SwapId, request.InitiatorId ?? string.Empty, request.PartnerId ?? string.Empty), ct);
        SetETag(conversation.Version);
        return CreatedAtAction(nameof(GetById), new { id = conversation.Id }, conversation);
    }

    [HttpPut("{id}/status")]
    [EndpointSummary("Completes or cancels the swap of the conversation and posts a system message.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(
        string id, ChangeSwapStatusRequest request,
        [FromHeader(Name = IfMatchHeader)] string? ifMatch, CancellationToken ct)
    {
        await Sender.Send(new ChangeSwapStatusCommand(id, UserId, request.Status, ParseVersion(ifMatch)), ct);
        return NoContent();
    }

    [HttpPost("{id}/read")]
    [EndpointSummary("Marks the conversation as read by the current user.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkRead(string id, CancellationToken ct)
    {
        await Sender.Send(new MarkConversationReadCommand(id, UserId), ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Deletes a closed conversation together with its messages and attachments.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        string id, [FromHeader(Name = IfMatchHeader)] string? ifMatch, CancellationToken ct)
    {
        await Sender.Send(new DeleteConversationCommand(id, UserId, ParseVersion(ifMatch)), ct);
        return NoContent();
    }
}
