using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Models;
using FlowChat.Shared.Domain;
using AccountAggregate = FlowChat.AuthService.Domain.Entities.Account.Account;

namespace FlowChat.AuthService.Application.Features.User.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : AggregateRootCommandHandlerBase<RefreshTokenCommand, RefreshTokenCommandResponse>
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

    private AccountAggregate? _account;

    protected override async Task<FlowChatResult<RefreshTokenCommandResponse>> ExecuteAsync(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        _account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (_account is null || !_account.IsEmailConfirmed)
        {
            return FlowChatResult<RefreshTokenCommandResponse>.Failure(
                DomainError.Unauthorized("User not found or account is not confirmed."));
        }

        var authenticatedAccount = new AuthenticatedAccount
        {
            Id = _account.Id.Value,
            FriendlyUserId = _account.FriendlyUserId.Value,
            Email = _account.Email.Value,
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

    protected override IAggregateRoot? GetAggregateRoot() => _account;
}
