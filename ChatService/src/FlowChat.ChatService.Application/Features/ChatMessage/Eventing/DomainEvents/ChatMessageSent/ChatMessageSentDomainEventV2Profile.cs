using AutoMapper;
using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;

public sealed class ChatMessageSentDomainEventV2Profile : Profile
{
    public ChatMessageSentDomainEventV2Profile()
    {
        CreateMap<ChatMessageSentDomainEventV2, ChatMessageSentIntegrationEventV2>()
            .ForMember(x => x.ConversationId, options => options.MapFrom(x => x.ConversationId.Value))
            .ForMember(x => x.SenderUserId, options => options.MapFrom(x => x.SenderUserId.Value))
            .ForMember(x => x.SentAtUtc, options => options.MapFrom(x => x.SentAtUtc.Value))
            .ForMember(x => x.ConversationMembershipRevision, options => options.Ignore());
    }
}
