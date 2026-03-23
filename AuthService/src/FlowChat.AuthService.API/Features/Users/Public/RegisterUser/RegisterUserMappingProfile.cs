using FlowChat.AuthService.Application.Users.Commands.RegisterUser;

namespace FlowChat.AuthService.API.Features.Users.Public.RegisterUser;

public sealed class RegisterUserMappingProfile : Profile
{
    public RegisterUserMappingProfile()
    {
        CreateMap<RegisterUserRequest, RegisterUserCommand>();
        CreateMap<RegisterUserCommandResponse, RegisterUserResponse>();
    }
}
