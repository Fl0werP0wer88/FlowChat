using FlowChat.AuthService.Application.Commands;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Responses;
using MediatR;

namespace FlowChat.AuthService.Application.Handlers;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginUserCommandResponse>
{
    private readonly IIdentityRepository _identityRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUserCommandHandler(IIdentityRepository identityRepository, IJwtTokenGenerator jwtTokenGenerator)
    {
        _identityRepository = identityRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginUserCommandResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityRepository.AuthenticateUserAsync(request.Login, request.Password, cancellationToken);
        if (user is null)
        {
            return new LoginUserCommandResponse { IsSuccess = false };
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new LoginUserCommandResponse
        {
            IsSuccess = true,
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc
        };
    }
}
