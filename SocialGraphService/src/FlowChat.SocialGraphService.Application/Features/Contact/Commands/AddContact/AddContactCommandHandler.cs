using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using ContactAggregate = FlowChat.SocialGraphService.Domain.Entities.Contact.Contact;
using UserProfileMarker = FlowChat.SocialGraphService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;

public sealed class AddContactCommandHandler
    : AggregateRootInsertCommandHandlerBaseV2<AddContactCommand, Guid, ContactAggregate>
{
    private readonly IContactWriteRepository _contactWriteRepository;
    private readonly IUserProfileProjectionReadRepository _userProfileProjectionReadRepository;
    private ContactAggregate? _contact;

    public AddContactCommandHandler(
        IContactWriteRepository contactWriteRepository,
        IUserProfileProjectionReadRepository userProfileProjectionReadRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<AddContactCommand, ContactAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _contactWriteRepository = contactWriteRepository;
        _userProfileProjectionReadRepository = userProfileProjectionReadRepository;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        AddContactCommand request,
        CancellationToken cancellationToken)
    {
        var projection = await GetUserProfileProjectionAsync(request, cancellationToken);
        if (projection is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound("User profile projection was not found."));
        }

        var ownerUserId = Id<UserProfileMarker>.FromGuid(request.OwnerUserId);
        var contactUserId = Id<UserProfileMarker>.FromGuid(projection.UserProfileId);

        if (contactUserId == ownerUserId)
        {
            return FlowChatResult<Guid>.Failure(DomainError.BadRequest("OwnerUserId and ContactUserId must be different."));
        }

        var contactAlreadyExists = await _contactWriteRepository.ExistsAsync(
            ownerUserId,
            contactUserId,
            cancellationToken);

        if (contactAlreadyExists)
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict("Contact already exists."));
        }

        _contact = ContactAggregate.Create(
            Id<ContactAggregate>.FromGuid(request.Id),
            ownerUserId,
            contactUserId,
            CreateDisplayName(projection),
            projection.FirstName,
            projection.LastName,
            CreatePhoneNumber(projection),
            CreateEmailAddress(projection));

        await _contactWriteRepository.AddAsync(_contact, cancellationToken);

        return FlowChatResult<Guid>.Success(_contact.Id.Value);
    }

    protected override ContactAggregate GetAggregateRoot() =>
        _contact ?? throw new InvalidOperationException("Aggregate root instance is not available.");

    private async Task<UserProfileProjectionDto?> GetUserProfileProjectionAsync(
        AddContactCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId.HasValue)
        {
            return await _userProfileProjectionReadRepository.GetByUserProfileIdAsync(
                request.UserId.Value,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.FriendlyUserId))
        {
            return await _userProfileProjectionReadRepository.GetByFriendlyUserIdAsync(
                request.FriendlyUserId.Trim(),
                cancellationToken);
        }

        return await _userProfileProjectionReadRepository.GetByEmailAsync(
            EmailAddress.Create(request.Email!).Value,
            cancellationToken);
    }

    private static EmailAddress? CreateEmailAddress(UserProfileProjectionDto projection)
    {
        if (!EmailAddress.TryCreate(projection.MainEmail?.Address, out var emailAddress))
        {
            return null;
        }

        return emailAddress;
    }

    private static PhoneNumber? CreatePhoneNumber(UserProfileProjectionDto projection)
    {
        if (!PhoneNumber.TryCreate(projection.MainPhone?.Number, out var phoneNumber))
        {
            return null;
        }

        return phoneNumber;
    }

    private static string CreateDisplayName(UserProfileProjectionDto projection)
    {
        var firstName = NormalizeOptional(projection.FirstName);
        var lastName = NormalizeOptional(projection.LastName);
        var displayName = $"{firstName} {lastName}".Trim();

        return string.IsNullOrWhiteSpace(displayName)
            ? projection.FriendlyUserId
            : displayName;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

