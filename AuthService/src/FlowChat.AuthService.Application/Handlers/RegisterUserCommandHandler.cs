using FlowChat.AuthService.Application.Commands;
using FlowChat.Application.Abstractions;
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
    private readonly IKafkaEventPublisher<UserEmailVerificationRequestedIntegrationEvent> _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(
        IIdentityRepository identityRepository,
        ITokenEncoder tokenEncoder,
        IConfirmationLinkBuilder confirmationLinkBuilder,
        IKafkaEventPublisher<UserEmailVerificationRequestedIntegrationEvent> eventPublisher,
        IUnitOfWork unitOfWork)
    {
        _identityRepository = identityRepository;
        _tokenEncoder = tokenEncoder;
        _confirmationLinkBuilder = confirmationLinkBuilder;
        _eventPublisher = eventPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterUserCommandResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var domainUser = IdentityEntity.Create(Guid.NewGuid(), request.UserName, request.Email);
            var guid = await _identityRepository.CreateUserAsync(domainUser, request.Password, ct);
            var confirmationToken = await _identityRepository.GenerateEmailConfirmationTokenAsync(guid, ct);
            var encodedToken = _tokenEncoder.EncodeForUrl(confirmationToken);
            var confirmationLink = _confirmationLinkBuilder.BuildEmailConfirmationLink(guid, encodedToken);

            await _eventPublisher.PublishAsync(new UserEmailVerificationRequestedIntegrationEvent
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
