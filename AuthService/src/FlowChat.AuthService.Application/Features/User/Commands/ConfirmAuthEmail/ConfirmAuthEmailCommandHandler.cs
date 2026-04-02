using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.AuthService.Domain.Entities.Identity;
using MediatR;

namespace FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;

public sealed class ConfirmAuthEmailCommandHandler : CommandHandlerBase<ConfirmAuthEmailCommand, Unit>
{
    private readonly IIdentityRepository _identityRepository;
    private Identity? _domainUser;

    public ConfirmAuthEmailCommandHandler(
        IIdentityRepository identityRepository,
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork) : base(domainEventDispatcher, unitOfWork)
    {
        _identityRepository = identityRepository;
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(ConfirmAuthEmailCommand request, CancellationToken cancellationToken)
    {
        var emailAddress = request.EmailAddress?.Trim();
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return FlowChatResult<Unit>.Failure(DomainError.BadRequest("Email address is required."));
        }

        _domainUser = await _identityRepository.GetByEmailAsync(emailAddress, cancellationToken);
        if (_domainUser is null)
        {
            return FlowChatResult<Unit>.Failure(DomainError.NotFound("User was not found."));
        }

        if (_domainUser.EmailConfirmed)
        {
            return FlowChatResult<Unit>.Failure(DomainError.Conflict("Email is already confirmed."));
        }

        _domainUser.ConfirmEmail();
        await _identityRepository.UpdateAsync(_domainUser, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result)
    {
        return _domainUser;
    }
}
