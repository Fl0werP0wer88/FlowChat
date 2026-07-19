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

    public ConfirmAuthEmailCommandHandler(
        IAccountRepository accountRepository,
        ILocalEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessor<ConfirmAuthEmailCommand, DomainAccount>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _accountRepository = accountRepository;
    }

    protected override async Task<FlowChatResult<DomainAccount?>> FetchAggregateRootAsync(
        ConfirmAuthEmailCommand request,
        CancellationToken cancellationToken)
    {
        var emailAddress = EmailAddress.Create(request.EmailAddress);

        var account = await _accountRepository.GetByEmailAsync(emailAddress, cancellationToken);
        if (account is null)
        {
            return FlowChatResult<DomainAccount?>.Failure(DomainError.NotFound("User was not found."));
        }

        return FlowChatResult<DomainAccount?>.Success(account);
    }

    protected override async Task<FlowChatResult<AggregateMutation<Unit>>> ExecuteAsync(ConfirmAuthEmailCommand request, CancellationToken cancellationToken)
    {
        if (AggregateRoot!.IsEmailConfirmed)
        {
            return Failure(DomainError.Conflict("Email is already confirmed."));
        }

        AggregateRoot.ConfirmEmail();
        await _accountRepository.UpdateAsync(AggregateRoot, cancellationToken);

        return Updated(Unit.Value);
    }
}
