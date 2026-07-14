using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetContactsForUser;

public sealed class GetContactsForUserMappingProfile : Profile
{
    public GetContactsForUserMappingProfile()
    {
        CreateMap<ContactDto, ContactResponse>();
    }
}
