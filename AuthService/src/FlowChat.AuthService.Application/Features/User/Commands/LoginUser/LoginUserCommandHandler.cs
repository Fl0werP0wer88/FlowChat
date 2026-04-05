using System.Security.Claims;
using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Models;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.AuthService.Application.Features.User.Commands.LoginUser;

public sealed class LoginUserCommandHandler : ICommandHandler<LoginUserCommand, LoginUserCommandResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IOpenIddictTokenService _openIddictTokenService;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IUnitOfWork _unitOfWork;

    public LoginUserCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHashingService passwordHashingService,
        IOpenIddictTokenService openIddictTokenService,
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _passwordHashingService = passwordHashingService;
        _openIddictTokenService = openIddictTokenService;
        _domainEventDispatcher = domainEventDispatcher;
        _unitOfWork = unitOfWork;
    }

    public Task<FlowChatResult<LoginUserCommandResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var account = await _accountRepository.GetByLoginAsync(request.Login, token);
            // Treat unconfirmed accounts as non-existent to prevent confirming account existence
            // before email verification is complete.
            if (account is null || !account.IsEmailConfirmed)
            {
                return FlowChatResult<LoginUserCommandResponse>.Failure(
                    DomainError.Unauthorized("Invalid credentials or account is not confirmed."));
            }

            var verificationResult = _passwordHashingService.VerifyHashedPassword(account.PasswordHash, request.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                account.RecordFailedLogin();
                await _accountRepository.UpdateAsync(account, token);
                await DispatchDomainEventsAsync(account, token);

                return FlowChatResult<LoginUserCommandResponse>.Failure(
                    DomainError.Unauthorized("Invalid credentials or account is not confirmed."));
            }

            account.ResetFailedLogins();
            await _accountRepository.UpdateAsync(account, token);
            await DispatchDomainEventsAsync(account, token);

            var authenticatedAccount = new AuthenticatedAccount
            {
                Id = account.Id.Value,
                FriendlyUserId = account.FriendlyUserId,
                Email = account.Email.Value,
                Roles = [] // Role-based authorization not yet implemented.
            };

            return FlowChatResult<LoginUserCommandResponse>.Success(
                new LoginUserCommandResponse
                {
                    Grant = new OpenIddictTokenGrantResult
                    {
                        Principal = _openIddictTokenService.CreatePrincipal(authenticatedAccount, request.Scopes)
                    }
                });
        }, cancellationToken);
    }

    private Task DispatchDomainEventsAsync(Account account, CancellationToken cancellationToken)
    {
        var domainEvents = account.PopDomainEvents();
        return _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}

