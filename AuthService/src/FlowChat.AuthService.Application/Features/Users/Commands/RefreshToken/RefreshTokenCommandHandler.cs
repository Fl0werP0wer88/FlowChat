using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Application.Features.Users.Commands.RefreshToken;

public class RefreshTokenCommandHandler : CommandHandlerBase<RefreshTokenCommand, RefreshTokenCommandResponse>
{
    private readonly IIdentityRepository _identityRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IIdentityRepository identityRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork) : base(domainEventDispatcher, unitOfWork)
    {
        _identityRepository = identityRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    protected override async Task<FlowChatResult<RefreshTokenCommandResponse>> ExecuteAsync(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _jwtTokenGenerator.ExtractUserIdFromExpiredToken(request.AccessToken);
        if (userId is null)
        {
            return FlowChatResult<RefreshTokenCommandResponse>.Failure(
                DomainError.Unauthorized("Invalid access token."));
        }

        var storedToken = await _identityRepository.GetRefreshTokenAsync(userId.Value, cancellationToken);
        if (storedToken is null
            || storedToken.Value.Token != request.RefreshToken
            || storedToken.Value.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return FlowChatResult<RefreshTokenCommandResponse>.Failure(
                DomainError.Unauthorized("Invalid or expired refresh token."));
        }

        var user = await _identityRepository.GetAuthenticatedUserByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return FlowChatResult<RefreshTokenCommandResponse>.Failure(
                DomainError.Unauthorized("User not found or account is not confirmed."));
        }

        await _identityRepository.RevokeRefreshTokenAsync(userId.Value, cancellationToken);

        var newToken = _jwtTokenGenerator.GenerateToken(user);

        await _identityRepository.SaveRefreshTokenAsync(
            user.Id, newToken.RefreshToken, newToken.RefreshTokenExpiresAtUtc, cancellationToken);

        return FlowChatResult<RefreshTokenCommandResponse>.Success(
            new RefreshTokenCommandResponse
            {
                AccessToken = newToken.AccessToken,
                ExpiresAtUtc = newToken.ExpiresAtUtc,
                RefreshToken = newToken.RefreshToken,
                RefreshTokenExpiresAtUtc = newToken.RefreshTokenExpiresAtUtc
            });
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<RefreshTokenCommandResponse> result)
    {
        return null;
    }
}
