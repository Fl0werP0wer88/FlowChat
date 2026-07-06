using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversationsForContacts;

public sealed class GetDuetConversationsForContactsMappingProfile : Profile
{
    public GetDuetConversationsForContactsMappingProfile()
    {
        CreateMap<DuetConversationForContactDto, DuetConversationForContactResponse>();
    }
}
