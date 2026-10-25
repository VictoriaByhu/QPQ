using MediatR;
using Microsoft.AspNetCore.Mvc;
using Messages.Application.Common.Models;
using Messages.Application.Messages.Commands.DeleteMessage;
using Messages.Application.Messages.Commands.EditTextMessage;
using Messages.Application.Messages.Commands.SendFileMessage;
using Messages.Application.Messages.Commands.SendTextMessage;
using Messages.Application.Messages.Dtos;
using Messages.Application.Messages.Queries.GetMessageById;
using Messages.Application.Messages.Queries.GetMessagesList;
using Messages.WebApi.Contracts;
using Messages.WebApi.Services;

namespace Messages.WebApi.Controllers;

[Route("api/messages")]
public sealed class MessagesController(ISender sender, ICurrentUser currentUser)
    : ApiControllerBase(sender, currentUser)
{
    [HttpGet("~/api/conversations/{conversationId}/messages")]
    [EndpointSummary("Chronological message feed of a conversation with optional full-text search.")]
    [ProducesResponseType<PagedResult<MessageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<MessageDto>>> GetList(
        string conversationId, [FromQuery] string? search, [FromQuery] string? senderId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await Sender.Send(
            new GetMessagesListQuery(conversationId, UserId, search, senderId, page, pageSize), ct));

    [HttpGet("{id}")]
    [EndpointSummary("Returns a message; the ETag header carries the document version.")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> GetById(string id, CancellationToken ct)
    {
        var message = await Sender.Send(new GetMessageByIdQuery(id, UserId), ct);
        SetETag(message.Version);
        return Ok(message);
    }

    [HttpPost("~/api/conversations/{conversationId}/messages/text")]
    [EndpointSummary("Sends a text message to the conversation.")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageDto>> SendText(
        string conversationId, SendTextMessageRequest request, CancellationToken ct)
    {
        var message = await Sender.Send(new SendTextMessageCommand(conversationId, UserId, request.Text), ct);
        SetETag(message.Version);
        return CreatedAtAction(nameof(GetById), new { id = message.Id }, message);
    }

    [HttpPost("~/api/conversations/{conversationId}/messages/file")]
    [EndpointSummary("Sends a message with attachments previously uploaded to the conversation.")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageDto>> SendFile(
        string conversationId, SendFileMessageRequest request, CancellationToken ct)
    {
        var message = await Sender.Send(
            new SendFileMessageCommand(conversationId, UserId, request.Caption, request.AttachmentIds), ct);
        SetETag(message.Version);
        return CreatedAtAction(nameof(GetById), new { id = message.Id }, message);
    }

    [HttpPut("{id}")]
    [EndpointSummary("Edits the text of an own message; If-Match enables the optimistic concurrency check.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Edit(
        string id, EditMessageRequest request,
        [FromHeader(Name = IfMatchHeader)] string? ifMatch, CancellationToken ct)
    {
        await Sender.Send(new EditTextMessageCommand(id, UserId, request.Text, ParseVersion(ifMatch)), ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Soft-deletes an own message.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        string id, [FromHeader(Name = IfMatchHeader)] string? ifMatch, CancellationToken ct)
    {
        await Sender.Send(new DeleteMessageCommand(id, UserId, ParseVersion(ifMatch)), ct);
        return NoContent();
    }
}
