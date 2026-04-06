using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;

public class RegisterUserCommandHandler : CommandHandlerBase<RegisterUserCommand, RegisterUserCommandResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private Account? _account;

    public RegisterUserCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHashingService passwordHashingService,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _accountRepository = accountRepository;
        _passwordHashingService = passwordHashingService;
    }

    protected override async Task<FlowChatResult<RegisterUserCommandResponse>> ExecuteAsync(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var emailAddress = EmailAddress.Create(request.Email);
        if (await _accountRepository.GetByEmailAsync(emailAddress, cancellationToken) is not null)
        {
            return FlowChatResult<RegisterUserCommandResponse>.Failure(
                DomainError.Conflict("Account with the provided email already exists."));
        }

        if (await _accountRepository.GetByFriendlyUserIdAsync(request.FriendlyUserId, cancellationToken) is not null)
        {
            return FlowChatResult<RegisterUserCommandResponse>.Failure(
                DomainError.Conflict("Account with the provided friendly user id already exists."));
        }

        _account = Account.Create(
            request.FriendlyUserId,
            emailAddress,
            _passwordHashingService.HashPassword(request.Password),
            _passwordHashingService.GenerateSecurityStamp(),
            request.FirstName,
            request.LastName,
            request.Organization);

        await _accountRepository.CreateAsync(_account, cancellationToken);

        return FlowChatResult<RegisterUserCommandResponse>.Success(
            new RegisterUserCommandResponse
            {
                Id = _account.Id.Value
            });
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<RegisterUserCommandResponse> result)
    {
        return _account;
    }
}

