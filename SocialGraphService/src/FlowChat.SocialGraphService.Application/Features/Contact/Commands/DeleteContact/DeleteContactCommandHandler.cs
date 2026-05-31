using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using ContactAggregate = FlowChat.SocialGraphService.Domain.Entities.Contact.Contact;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Commands.DeleteContact;

public sealed class DeleteContactCommandHandler : AggregateRootCommandHandlerBase<DeleteContactCommand, MediatR.Unit>
{
    private readonly IContactWriteRepository _contactWriteRepository;
    private ContactAggregate? _contact;

    public DeleteContactCommandHandler(
        IContactWriteRepository contactWriteRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher, unitOfWork)
    {
        _contactWriteRepository = contactWriteRepository ?? throw new ArgumentNullException(nameof(contactWriteRepository));
    }

    protected override async Task<FlowChatResult<MediatR.Unit>> ExecuteAsync(
        DeleteContactCommand request,
        CancellationToken cancellationToken)
    {
        _contact = await _contactWriteRepository.GetByOwnerAndContactAsync(
            request.OwnerUserId,
            request.ContactUserId,
            cancellationToken);

        if (_contact is null)
        {
            return FlowChatResult<MediatR.Unit>.Failure(DomainError.NotFound("Contact was not found."));
        }

        _contact.MarkDeleted();
        await _contactWriteRepository.DeleteAsync(_contact, cancellationToken);

        return FlowChatResult<MediatR.Unit>.Success(MediatR.Unit.Value);
    }

    protected override IAggregateRoot GetAggregateRoot() =>
        _contact ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
