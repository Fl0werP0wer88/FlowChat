using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;

namespace FlowChat.AuthService.Application.Features.User.Commands.ChangeAuthEmail;

public sealed class ChangeAuthEmailCommandHandler : CommandHandlerBase<ChangeAuthEmailCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private Account? _account;

    public ChangeAuthEmailCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHashingService passwordHashingService,
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork) : base(domainEventDispatcher, unitOfWork)
    {
        _accountRepository = accountRepository;
        _passwordHashingService = passwordHashingService;
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(ChangeAuthEmailCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("UserId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.EmailAddress))
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("Email address is required."));
        }

        if (!EmailAddress.TryCreate(request.EmailAddress, out var emailAddress))
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest(EmailAddress.InvalidEmailAddressMessage));
        }

        _account = await _accountRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_account is null)
        {
            return FlowChatResult<Unit>.Failure(DomainError.NotFound("User was not found."));
        }

        if (_account.Email == emailAddress)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        var existingAccount = await _accountRepository.GetByEmailAsync(emailAddress, cancellationToken);
        if (existingAccount is not null && existingAccount.Id != _account.Id)
        {
            return FlowChatResult<Unit>.Failure(DomainError.Conflict("Email is already in use."));
        }

        _account.ChangeAuthEmail(emailAddress, _passwordHashingService.GenerateSecurityStamp());
        await _accountRepository.UpdateAsync(_account, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result)
    {
        return result.IsSuccess ? _account : null;
    }
}
