using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using ContactAggregate = FlowChat.SocialGraphService.Domain.Entities.Contact.Contact;
using UserProfileMarker = FlowChat.SocialGraphService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Commands.DeleteContact;

public sealed class DeleteContactCommandHandler
    : AggregateRootDeleteCommandHandlerBaseV3<DeleteContactCommand, MediatR.Unit, ContactAggregate>
{
    private readonly IContactWriteRepository _contactWriteRepository;

    public DeleteContactCommandHandler(
        IContactWriteRepository contactWriteRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<DeleteContactCommand, ContactAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _contactWriteRepository = contactWriteRepository ?? throw new ArgumentNullException(nameof(contactWriteRepository));
    }

    protected override async Task<FlowChatResult<ContactAggregate?>> FetchAggregateRootAsync(
        DeleteContactCommand request,
        CancellationToken cancellationToken)
    {
        var contact = await _contactWriteRepository.GetByOwnerAndContactAsync(
            Id<UserProfileMarker>.FromGuid(request.OwnerUserId),
            Id<UserProfileMarker>.FromGuid(request.ContactUserId),
            cancellationToken);

        if (contact is null)
        {
            return FlowChatResult<ContactAggregate?>.Failure(DomainError.NotFound("Contact was not found."));
        }

        return FlowChatResult<ContactAggregate?>.Success(contact);
    }

    protected override async Task<FlowChatResult<MediatR.Unit>> ExecuteAsync(
        DeleteContactCommand request,
        CancellationToken cancellationToken)
    {
        await _contactWriteRepository.SoftDeleteAsync(AggregateRoot!, cancellationToken);
        SetDeleted();

        return FlowChatResult<MediatR.Unit>.Success(MediatR.Unit.Value);
    }
}
