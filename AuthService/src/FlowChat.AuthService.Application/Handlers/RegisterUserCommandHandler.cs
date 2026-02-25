using FlowChat.AuthService.Application.Commands;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Responses;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.Messaging.Contracts.AuthService.Events;
using MediatR;

namespace FlowChat.AuthService.Application.Handlers;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserCommandResponse>
{
    private readonly IIdentityRepository _identityRepository;
    private readonly ITokenEncoder _tokenEncoder;
    private readonly IConfirmationLinkBuilder _confirmationLinkBuilder;
    private readonly IOutboxRepository<UserEmailVerificationRequestedIntegrationEvent> _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(
        IIdentityRepository identityRepository,
        ITokenEncoder tokenEncoder,
        IConfirmationLinkBuilder confirmationLinkBuilder,
        IOutboxRepository<UserEmailVerificationRequestedIntegrationEvent> outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _identityRepository = identityRepository;
        _tokenEncoder = tokenEncoder;
        _confirmationLinkBuilder = confirmationLinkBuilder;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterUserCommandResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var domainUser =  UserEntity.Create(Guid.NewGuid(), request.UserName, request.Email);
            var guid = await _identityRepository.CreateUserAsync(domainUser, request.Password, ct);
            var confirmationToken = await _identityRepository.GenerateEmailConfirmationTokenAsync(guid, ct);
            var encodedToken = _tokenEncoder.EncodeForUrl(confirmationToken);
            var confirmationLink = _confirmationLinkBuilder.BuildEmailConfirmationLink(guid, encodedToken);

            await _outboxRepository.EnqueueAsync(new UserEmailVerificationRequestedIntegrationEvent
            {
                UserId = guid,
                UserEmail = request.Email,
                ConfirmationLink = confirmationLink
            }, ct);

            return new RegisterUserCommandResponse
            {
                Id = guid
            };
        }, cancellationToken);
    }
}
