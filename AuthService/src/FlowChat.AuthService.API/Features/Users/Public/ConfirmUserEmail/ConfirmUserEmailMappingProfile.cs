using FlowChat.AuthService.Application.Features.Users.Commands.ConfirmUserEmail;

namespace FlowChat.AuthService.API.Features.Users.Public.ConfirmUserEmail;

public sealed class ConfirmUserEmailMappingProfile : Profile
{
    public ConfirmUserEmailMappingProfile()
    {
        CreateMap<ConfirmUserEmailRequest, ConfirmUserEmailCommand>();
    }
}
