using CSharpFunctionalExtensions;
using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Application.Features.Users.Commands.LoginUser;

public class LoginUserCommandHandler : CommandHandlerBase<LoginUserCommand, LoginUserCommandResponse>
{
    private readonly IIdentityRepository _identityRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUserCommandHandler(
        IIdentityRepository identityRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork) : base(domainEventDispatcher, unitOfWork)
    {
        _identityRepository = identityRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    protected override async Task<FlowChatResult<LoginUserCommandResponse>> ExecuteAsync(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        var user = await _identityRepository.LoginUserAsync(
            request.Login,
            request.Password,
            refreshToken.Token,
            refreshToken.ExpiresAtUtc,
            cancellationToken);
        if (user is null)
        {
            return FlowChatResult<LoginUserCommandResponse>.Failure(
                DomainError.Unauthorized("Invalid credentials or account is not confirmed."));
        }

        var token = _jwtTokenGenerator.GenerateToken(user, refreshToken.Token, refreshToken.ExpiresAtUtc);

        return FlowChatResult<LoginUserCommandResponse>.Success(
            new LoginUserCommandResponse
            {
                IsSuccess = true,
                AccessToken = token.AccessToken,
                ExpiresAtUtc = token.ExpiresAtUtc,
                RefreshToken = token.RefreshToken,
                RefreshTokenExpiresAtUtc = token.RefreshTokenExpiresAtUtc
            });
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<LoginUserCommandResponse> result)
    {
        return null;
    }
}

