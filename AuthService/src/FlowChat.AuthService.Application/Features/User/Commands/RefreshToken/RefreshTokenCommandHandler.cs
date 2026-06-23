using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Models;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Application.Features.User.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : CommandHandlerBase<RefreshTokenCommand, RefreshTokenCommandResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOpenIddictTokenService _openIddictTokenService;

    public RefreshTokenCommandHandler(
        IAccountRepository accountRepository,
        IOpenIddictTokenService openIddictTokenService,
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork) : base(domainEventDispatcher, unitOfWork)
    {
        _accountRepository = accountRepository;
        _openIddictTokenService = openIddictTokenService;
    }

    protected override async Task<FlowChatResult<RefreshTokenCommandResponse>> ExecuteAsync(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null || !account.IsEmailConfirmed)
        {
            return FlowChatResult<RefreshTokenCommandResponse>.Failure(
                DomainError.Unauthorized("User not found or account is not confirmed."));
        }

        var authenticatedAccount = new AuthenticatedAccount
        {
            Id = account.Id.Value,
            FriendlyUserId = account.FriendlyUserId.Value,
            Email = account.Email.Value,
            Roles = []
        };

        return FlowChatResult<RefreshTokenCommandResponse>.Success(
            new RefreshTokenCommandResponse
            {
                Grant = new OpenIddictTokenGrantResult
                {
                    Principal = _openIddictTokenService.CreatePrincipal(authenticatedAccount, request.Scopes)
                }
            });
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<RefreshTokenCommandResponse> result)
    {
        return null;
    }
}
