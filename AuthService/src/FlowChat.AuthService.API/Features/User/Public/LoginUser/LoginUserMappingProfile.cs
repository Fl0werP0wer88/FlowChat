using FlowChat.AuthService.Application.Features.User.Commands.LoginUser;

namespace FlowChat.AuthService.API.Features.User.Public.LoginUser;

public sealed class LoginUserMappingProfile : Profile
{
    public LoginUserMappingProfile()
    {
        CreateMap<LoginUserRequest, LoginUserCommand>();
        CreateMap<LoginUserCommandResponse, LoginUserResponse>();
    }
}
