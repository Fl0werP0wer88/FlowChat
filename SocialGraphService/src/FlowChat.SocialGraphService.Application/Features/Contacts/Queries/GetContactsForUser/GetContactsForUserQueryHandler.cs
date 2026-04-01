using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.Shared.Domain;

namespace FlowChat.SocialGraphService.Application.Features.Contacts.Queries.GetContactsForUser;

public sealed class GetContactsForUserQueryHandler : IQueryHandler<GetContactsForUserQuery, IReadOnlyList<ContactDto>>
{
    private readonly IContactReadRepository _contactReadRepository;

    public GetContactsForUserQueryHandler(IContactReadRepository contactReadRepository)
    {
        _contactReadRepository = contactReadRepository;
    }

    public async Task<FlowChatResult<IReadOnlyList<ContactDto>>> Handle(
        GetContactsForUserQuery request,
        CancellationToken cancellationToken)
    {
        var contacts = await _contactReadRepository.GetForUserAsync(
            request.UserId,
            cancellationToken);

        return FlowChatResult<IReadOnlyList<ContactDto>>.Success(contacts);
    }
}

