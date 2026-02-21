using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Contacts.Queries;

public sealed class GetContactsForUserQueryHandler
    : IRequestHandler<GetContactsForUserQuery, IReadOnlyList<ContactDto>>
{
    private readonly IContactRepository _contactRepository;

    public GetContactsForUserQueryHandler(IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<IReadOnlyList<ContactDto>> Handle(
        GetContactsForUserQuery request,
        CancellationToken cancellationToken)
    {
        var contacts = await _contactRepository.GetForUserAsync(
            request.UserId,
            request.Status,
            cancellationToken);

        return contacts
            .Select(x => new ContactDto(
                x.Id,
                x.UserId1,
                x.UserId2,
                x.IsBlocked,
                x.BlockedBy,
                x.CreatedDate,
                x.LastModifiedDate))
            .ToList();
    }
}
