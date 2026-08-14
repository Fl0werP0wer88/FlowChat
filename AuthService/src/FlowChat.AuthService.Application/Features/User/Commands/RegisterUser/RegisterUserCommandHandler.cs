using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using DomainAccount = FlowChat.AuthService.Domain.Entities.Account.Account;

namespace FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;

public class RegisterUserCommandHandler
    : AggregateRootInsertCommandHandlerBaseV3<RegisterUserCommand, RegisterUserCommandResponse, DomainAccount>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private DomainAccount? _account;

    public RegisterUserCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHashingService passwordHashingService,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<RegisterUserCommand, DomainAccount>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _accountRepository = accountRepository;
        _passwordHashingService = passwordHashingService;
    }

    protected override async Task<FlowChatResult<AggregateMutation<RegisterUserCommandResponse>>> ExecuteAsync(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var emailAddress = EmailAddress.Create(request.Email);
        if (await _accountRepository.GetByEmailAsync(emailAddress, cancellationToken) is not null)
        {
            return Failure(
                DomainError.Conflict("Account with the provided email already exists."));
        }

        if (await _accountRepository.GetByFriendlyUserIdAsync(request.FriendlyUserId, cancellationToken) is not null)
        {
            return Failure(
                DomainError.Conflict("Account with the provided friendly user id already exists."));
        }

        _account = DomainAccount.Create(
            Id<DomainAccount>.FromGuid(request.Id),
            request.FriendlyUserId,
            emailAddress,
            _passwordHashingService.HashPassword(request.Password),
            _passwordHashingService.GenerateSecurityStamp(),
            request.FirstName,
            request.LastName,
            request.Organization);

        await _accountRepository.CreateAsync(_account, cancellationToken);

        return Created(
            new RegisterUserCommandResponse
            {
                Id = _account.Id.Value
            });
    }

    protected override DomainAccount GetAggregateRoot() =>
        _account ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
