using AutoMapper;
using FlowChat.AuthService.Application.Features.Users.Commands.RefreshToken;

namespace FlowChat.AuthService.API.Features.Users.Public.RefreshToken;

public sealed class RefreshTokenMappingProfile : Profile
{
    public RefreshTokenMappingProfile()
    {
        CreateMap<RefreshTokenRequest, RefreshTokenCommand>();
        CreateMap<RefreshTokenCommandResponse, RefreshTokenResponse>();
    }
}
