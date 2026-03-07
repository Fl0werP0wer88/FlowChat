using FlowChat.AuthService.Application.Commands;
using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Responses;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Domain.Events;
using AutoMapper;
using CSharpFunctionalExtensions;
using FlowChat.Domain.Abstractions;
using FlowChat.Messaging.Contracts.AuthService.Events;

namespace FlowChat.AuthService.Application.Handlers;

public class RegisterUserCommandHandler : CommandHandlerBase<RegisterUserCommand, RegisterUserCommandResponse>
{
    private readonly IIdentityRepository _identityRepository;
    private readonly ITokenEncoder _tokenEncoder;
    private readonly IConfirmationLinkBuilder _confirmationLinkBuilder;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IMapper _mapper;
    private Identity? _domainUser;
    public RegisterUserCommandHandler(
        IIdentityRepository identityRepository,
        ITokenEncoder tokenEncoder,
        IConfirmationLinkBuilder confirmationLinkBuilder,
        IIntegrationEventPublisher eventPublisher,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _identityRepository = identityRepository;
        _tokenEncoder = tokenEncoder;
        _confirmationLinkBuilder = confirmationLinkBuilder;
        _eventPublisher = eventPublisher;
        _mapper = mapper;
    }

    protected override async Task<Result<RegisterUserCommandResponse, IDomainError>> ExecuteAsync(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        _domainUser = Identity.Create(Guid.NewGuid(), request.UserName, request.Email, request.PhoneNumber);
        var guid = await _identityRepository.CreateUserAsync(_domainUser, request.Password, cancellationToken);
        var confirmationToken = await _identityRepository.GenerateEmailConfirmationTokenAsync(guid, cancellationToken);
        var encodedToken = _tokenEncoder.EncodeForUrl(confirmationToken);
        var confirmationLink = _confirmationLinkBuilder.BuildEmailConfirmationLink(guid, encodedToken);

        var emailVerificationRequested = new EmailVerificationRequestedDomainEvent(
            guid,
            request.Email,
            confirmationLink);

        await _eventPublisher.PublishToOutboxAsync(
            _mapper.Map<EmailVerificationRequestIntegrationEvent>(emailVerificationRequested),
            cancellationToken);

        return new RegisterUserCommandResponse
        {
            Id = guid
        };
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<RegisterUserCommandResponse, IDomainError> result)
    {
        return _domainUser;
    }
}
