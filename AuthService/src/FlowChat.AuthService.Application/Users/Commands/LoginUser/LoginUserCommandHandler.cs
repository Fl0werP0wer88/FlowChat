using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.Domain.Abstractions;

namespace FlowChat.AuthService.Application.Users.Commands.LoginUser;

public class LoginUserCommandHandler : CommandHandlerBase<LoginUserCommand, LoginUserCommandResponse>
{
    private readonly IIdentityRepository _identityRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUserCommandHandler(
        IIdentityRepository identityRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork) : base(domainEventDispatcher, unitOfWork)
    {
        _identityRepository = identityRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    protected override async Task<Result<LoginUserCommandResponse, IDomainError>> ExecuteAsync(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityRepository.AuthenticateUserAsync(request.Login, request.Password, cancellationToken);
        if (user is null)
        {
            return Result.Failure<LoginUserCommandResponse, IDomainError>(
                DomainError.Unauthorized("Invalid credentials or account is not confirmed."));
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new LoginUserCommandResponse
        {
            IsSuccess = true,
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc
        };
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<LoginUserCommandResponse, IDomainError> result)
    {
        return null;
    }
}
