using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Application.Commands;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Responses;
using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.AuthService.Application.Handlers;

public class ConfirmUserEmailCommandHandler :  CommandHandlerBase<ConfirmUserEmailCommand, ConfirmUserEmailCommandResponse>
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

    protected override async Task<Result<ConfirmUserEmailCommandResponse, IDomainError>> ExecuteAsync(ConfirmUserEmailCommand request, CancellationToken cancellationToken)
    {
        string decodedToken;
        try
        {
            decodedToken = _tokenEncoder.DecodeFromUrl(request.Token);
        }
        catch (FormatException)
        {
            return Result.Failure<ConfirmUserEmailCommandResponse, IDomainError>(
                DomainError.BadRequest("Email confirmation token is invalid."));
        }
        catch (ArgumentException)
        {
            return Result.Failure<ConfirmUserEmailCommandResponse, IDomainError>(
                DomainError.BadRequest("Email confirmation token is invalid."));
        }

        _domainUser = await _identityRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_domainUser is null)
        {
            return Result.Failure<ConfirmUserEmailCommandResponse, IDomainError>(
                DomainError.NotFound("User was not found."));
        }

        var isTokenValid = await _identityRepository.IsEmailConfirmationTokenValidAsync(request.UserId, decodedToken, cancellationToken);
        if (!isTokenValid)
        {
            return Result.Failure<ConfirmUserEmailCommandResponse, IDomainError>(
                DomainError.BadRequest("Email confirmation failed."));
        }

        _domainUser.ConfirmEmail();
        await _identityRepository.UpdateAsync(_domainUser, cancellationToken);

        return new ConfirmUserEmailCommandResponse
        {
            IsSuccess = true
        };
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<ConfirmUserEmailCommandResponse, IDomainError> result)
    {
        return _domainUser;
    }
}
