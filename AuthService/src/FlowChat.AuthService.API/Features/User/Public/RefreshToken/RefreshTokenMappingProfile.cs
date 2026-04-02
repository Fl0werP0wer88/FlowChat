using AutoMapper;
using FlowChat.AuthService.Application.Features.User.Commands.RefreshToken;

namespace FlowChat.AuthService.API.Features.User.Public.RefreshToken;

public sealed class RefreshTokenMappingProfile : Profile
{
    public RefreshTokenMappingProfile()
    {
        CreateMap<RefreshTokenRequest, RefreshTokenCommand>();
        CreateMap<RefreshTokenCommandResponse, RefreshTokenResponse>();
    }
}
