using AutoMapper;
using MongoDB.Bson;
using Messages.Application.Attachments.Dtos;
using Messages.Application.Conversations.Dtos;
using Messages.Application.Messages.Dtos;
using Messages.Domain.Entities;
using Messages.Domain.ValueObjects;

namespace Messages.Application.Common.Mapping;

public sealed class MessagesProfile : Profile
{
    public MessagesProfile()
    {
        CreateMap<ObjectId, string>().ConvertUsing(id => id.ToString());

        CreateMap<Participant, ParticipantDto>();
        CreateMap<LastMessage, LastMessageDto>();
        CreateMap<Conversation, ConversationDto>();

        CreateMap<AttachmentRef, AttachmentRefDto>();
        CreateMap<TextMessage, MessageDto>();
        CreateMap<FileMessage, MessageDto>();
        CreateMap<SystemMessage, MessageDto>()
            .ForMember(d => d.SwapId, o => o.MapFrom(s => s.Details.SwapId));

        CreateMap<Attachment, AttachmentDto>();
    }
}
