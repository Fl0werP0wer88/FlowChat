using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.AuthService.Domain.Entities.Account;
using MediatR;
using DomainAccount = FlowChat.AuthService.Domain.Entities.Account.Account;

namespace FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;

public sealed class ConfirmAuthEmailCommandHandler : AggregateRootCommandHandlerBase<ConfirmAuthEmailCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private DomainAccount? _account;

    public ConfirmAuthEmailCommandHandler(
        IAccountRepository accountRepository,
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork) : base(domainEventDispatcher, unitOfWork)
    {
        _accountRepository = accountRepository;
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(ConfirmAuthEmailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EmailAddress))
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("Email address is required."));
        }

        if (!EmailAddress.TryCreate(request.EmailAddress, out var emailAddress))
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest(EmailAddress.InvalidEmailAddressMessage));
        }

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

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result)
    {
        return _account;
    }
}
