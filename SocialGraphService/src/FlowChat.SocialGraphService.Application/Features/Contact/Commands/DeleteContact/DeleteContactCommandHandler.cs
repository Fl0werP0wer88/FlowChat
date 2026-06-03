using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using ContactAggregate = FlowChat.SocialGraphService.Domain.Entities.Contact.Contact;
using UserProfileMarker = FlowChat.SocialGraphService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Commands.DeleteContact;

public sealed class DeleteContactCommandHandler
    : AggregateRootDeleteCommandHandlerBaseV2<DeleteContactCommand, MediatR.Unit, ContactAggregate>
{
    private readonly IContactWriteRepository _contactWriteRepository;
    private ContactAggregate? _contact;

    public DeleteContactCommandHandler(
        IContactWriteRepository contactWriteRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<DeleteContactCommand, ContactAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _contactWriteRepository = contactWriteRepository ?? throw new ArgumentNullException(nameof(contactWriteRepository));
    }

    protected override async Task<FlowChatResult<MediatR.Unit>> ExecuteAsync(
        DeleteContactCommand request,
        CancellationToken cancellationToken)
    {
        _contact = await _contactWriteRepository.GetByOwnerAndContactAsync(
            Id<UserProfileMarker>.FromGuid(request.OwnerUserId),
            Id<UserProfileMarker>.FromGuid(request.ContactUserId),
            cancellationToken);

        if (_contact is null)
        {
            return FlowChatResult<MediatR.Unit>.Failure(DomainError.NotFound("Contact was not found."));
        }

        _contact.MarkDeleted();
        await _contactWriteRepository.SoftDeleteAsync(_contact, cancellationToken);

        return FlowChatResult<MediatR.Unit>.Success(MediatR.Unit.Value);
    }

    protected override ContactAggregate GetAggregateRoot() =>
        _contact ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
