using CSharpFunctionalExtensions;
using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Application.Features.Users.Commands.LoginUser;

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

    protected override async Task<FlowChatResult<LoginUserCommandResponse>> ExecuteAsync(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityRepository.AuthenticateUserAsync(request.Login, request.Password, cancellationToken);
        if (user is null)
        {
            return FlowChatResult<LoginUserCommandResponse>.Failure(
                DomainError.Unauthorized("Invalid credentials."));
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return FlowChatResult<LoginUserCommandResponse>.Success(
            new LoginUserCommandResponse
            {
                IsSuccess = true,
                AccessToken = token.AccessToken,
                ExpiresAtUtc = token.ExpiresAtUtc
            });
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<LoginUserCommandResponse> result)
    {
        return null;
    }
}

