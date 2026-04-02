using FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;

namespace FlowChat.AuthService.API.Features.User.Public.RegisterUser;

public sealed class RegisterUserMappingProfile : Profile
{
    public RegisterUserMappingProfile()
    {
        CreateMap<RegisterUserRequest, RegisterUserCommand>();
        CreateMap<RegisterUserCommandResponse, RegisterUserResponse>();
    }
}
