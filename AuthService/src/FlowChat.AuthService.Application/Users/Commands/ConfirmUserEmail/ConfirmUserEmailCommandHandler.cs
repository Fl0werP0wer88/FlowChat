using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.AuthService.Application.Users.Commands.ConfirmUserEmail;

public class ConfirmUserEmailCommandHandler : CommandHandlerBase<ConfirmUserEmailCommand, Unit>
{
    private readonly IIdentityRepository _identityRepository;
    private readonly ITokenEncoder _tokenEncoder;
    private Domain.Entities.Identity? _domainUser;

    public ConfirmUserEmailCommandHandler(
        IIdentityRepository identityRepository,
        ITokenEncoder tokenEncoder,
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork) : base(domainEventDispatcher, unitOfWork)
    {
        _identityRepository = identityRepository;
        _tokenEncoder = tokenEncoder;
    }

    protected override async Task<Result<Unit, IDomainError>> ExecuteAsync(ConfirmUserEmailCommand request, CancellationToken cancellationToken)
    {
        string decodedToken;
        try
        {
            decodedToken = _tokenEncoder.DecodeFromUrl(request.Token);
        }
        catch (FormatException)
        {
            return Result.Failure<Unit, IDomainError>(
                DomainError.BadRequest("Email confirmation token is invalid."));
        }
        catch (ArgumentException)
        {
            return Result.Failure<Unit, IDomainError>(
                DomainError.BadRequest("Email confirmation token is invalid."));
        }

        _domainUser = await _identityRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_domainUser is null)
        {
            return Result.Failure<Unit, IDomainError>(
                DomainError.NotFound("User was not found."));
        }

        if (_domainUser.EmailConfirmed)
        {
            return Result.Failure<Unit, IDomainError>(
                DomainError.Conflict("Email has already been confirmed."));
        }

        var isTokenValid = await _identityRepository.IsEmailConfirmationTokenValidAsync(request.UserId, decodedToken, cancellationToken);
        if (!isTokenValid)
        {
            return Result.Failure<Unit, IDomainError>(
                DomainError.BadRequest("Email confirmation failed."));
        }

        _domainUser.ConfirmEmail();
        await _identityRepository.UpdateAsync(_domainUser, cancellationToken);

        return Unit.Value;
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<Unit, IDomainError> result)
    {
        return _domainUser;
    }
}
