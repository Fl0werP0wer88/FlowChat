using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.AuthService.Domain.Entities.Account;
using MediatR;
using DomainAccount = FlowChat.AuthService.Domain.Entities.Account.Account;

namespace FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;

public sealed class ConfirmAuthEmailCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV3<ConfirmAuthEmailCommand, Unit, DomainAccount>
{
    private readonly IAccountRepository _accountRepository;
    private DomainAccount? _account;

    public ConfirmAuthEmailCommandHandler(
        IAccountRepository accountRepository,
        ILocalEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessor<ConfirmAuthEmailCommand, DomainAccount>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _accountRepository = accountRepository;
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(ConfirmAuthEmailCommand request, CancellationToken cancellationToken)
    {
        var emailAddress = EmailAddress.Create(request.EmailAddress);

        _account = await _accountRepository.GetByEmailAsync(emailAddress, cancellationToken);
        if (_account is null)
        {
            return FlowChatResult<Unit>.Failure(DomainError.NotFound("User was not found."));
        }

        if (_account.IsEmailConfirmed)
        {
            return FlowChatResult<Unit>.Failure(DomainError.Conflict("Email is already confirmed."));
        }

        _account.ConfirmEmail();
        await _accountRepository.UpdateAsync(_account, cancellationToken);
        SetUpdated();

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override DomainAccount GetAggregateRoot() =>
        _account ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
