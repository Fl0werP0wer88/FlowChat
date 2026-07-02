using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;
using DomainAccount = FlowChat.AuthService.Domain.Entities.Account.Account;

namespace FlowChat.AuthService.Application.Features.User.Commands.ChangeAuthEmail;

public sealed class ChangeAuthEmailCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV3<ChangeAuthEmailCommand, Unit, DomainAccount>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private DomainAccount? _account;

    public ChangeAuthEmailCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHashingService passwordHashingService,
        ILocalEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessor<ChangeAuthEmailCommand, DomainAccount>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _accountRepository = accountRepository;
        _passwordHashingService = passwordHashingService;
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(ChangeAuthEmailCommand request, CancellationToken cancellationToken)
    {
        var emailAddress = EmailAddress.Create(request.EmailAddress);

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
        SetUpdated();
        await _accountRepository.UpdateAsync(_account, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override DomainAccount GetAggregateRoot() =>
        _account ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
