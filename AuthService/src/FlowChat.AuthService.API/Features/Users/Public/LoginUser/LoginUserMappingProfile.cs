using FlowChat.AuthService.Application.Users.Commands.LoginUser;

namespace FlowChat.AuthService.API.Features.Users.Public.LoginUser;

public sealed class LoginUserMappingProfile : Profile
{
    public LoginUserMappingProfile()
    {
        CreateMap<LoginUserRequest, LoginUserCommand>();
        CreateMap<LoginUserCommandResponse, LoginUserResponse>();
    }
}
