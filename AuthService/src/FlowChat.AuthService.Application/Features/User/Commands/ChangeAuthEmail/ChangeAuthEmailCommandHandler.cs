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
    private EmailAddress? _emailAddress;

    public ChangeAuthEmailCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHashingService passwordHashingService,
        ILocalEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessorV2<ChangeAuthEmailCommand, DomainAccount>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _accountRepository = accountRepository;
        _passwordHashingService = passwordHashingService;
    }

    protected override async Task<FlowChatResult<DomainAccount?>> FetchAggregateRootAsync(
        ChangeAuthEmailCommand request,
        CancellationToken cancellationToken)
    {
        _emailAddress = EmailAddress.Create(request.EmailAddress);

        var account = await _accountRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (account is null)
        {
            return FlowChatResult<DomainAccount?>.Failure(DomainError.NotFound("User was not found."));
        }

        return FlowChatResult<DomainAccount?>.Success(account);
    }

    protected override async Task<FlowChatResult<AggregateMutation<Unit>>> ExecuteAsync(ChangeAuthEmailCommand request, CancellationToken cancellationToken)
    {
        if (AggregateRoot!.Email == _emailAddress)
        {
            return Unchanged(Unit.Value);
        }

        var existingAccount = await _accountRepository.GetByEmailAsync(_emailAddress!, cancellationToken);
        if (existingAccount is not null && existingAccount.Id != AggregateRoot.Id)
        {
            return Failure(DomainError.Conflict("Email is already in use."));
        }

        AggregateRoot.ChangeAuthEmail(_emailAddress!, _passwordHashingService.GenerateSecurityStamp());
        await _accountRepository.UpdateAsync(AggregateRoot, cancellationToken);

        return Updated(Unit.Value);
    }
}
