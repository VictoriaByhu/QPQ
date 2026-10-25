using MediatR;
using Microsoft.AspNetCore.Mvc;
using Messages.Application.Attachments.Commands.UploadAttachment;
using Messages.Application.Attachments.Dtos;
using Messages.Application.Attachments.Queries.GetAttachmentById;
using Messages.Application.Attachments.Queries.GetAttachmentsList;
using Messages.Application.Common.Models;
using Messages.WebApi.Contracts;
using Messages.WebApi.Services;

namespace Messages.WebApi.Controllers;

[Route("api/attachments")]
public sealed class AttachmentsController(ISender sender, ICurrentUser currentUser)
    : ApiControllerBase(sender, currentUser)
{
    [HttpGet("~/api/conversations/{conversationId}/attachments")]
    [EndpointSummary("Lists files of a conversation, newest first.")]
    [ProducesResponseType<PagedResult<AttachmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<AttachmentDto>>> GetList(
        string conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await Sender.Send(new GetAttachmentsListQuery(conversationId, UserId, page, pageSize), ct));

    [HttpGet("{id}")]
    [EndpointSummary("Returns attachment metadata.")]
    [ProducesResponseType<AttachmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttachmentDto>> GetById(string id, CancellationToken ct)
        => Ok(await Sender.Send(new GetAttachmentByIdQuery(id, UserId), ct));

    [HttpPost("~/api/conversations/{conversationId}/attachments")]
    [EndpointSummary("Registers metadata of a file uploaded to the file storage.")]
    [ProducesResponseType<AttachmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttachmentDto>> Upload(
        string conversationId, UploadAttachmentRequest request, CancellationToken ct)
    {
        var attachment = await Sender.Send(new UploadAttachmentCommand(
            conversationId, UserId, request.FileName, request.Url, request.ContentType, request.SizeBytes), ct);
        return CreatedAtAction(nameof(GetById), new { id = attachment.Id }, attachment);
    }
}
