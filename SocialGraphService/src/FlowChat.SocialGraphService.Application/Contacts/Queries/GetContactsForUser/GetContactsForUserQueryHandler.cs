using FlowChat.SocialGraphService.Application.Contracts;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Errors;

namespace FlowChat.SocialGraphService.Application.Contacts.Queries.GetContactsForUser;

public sealed class GetContactsForUserQueryHandler : IQueryHandler<GetContactsForUserQuery, IReadOnlyList<ContactDto>>
{
    private readonly IContactReadRepository _contactReadRepository;

    public GetContactsForUserQueryHandler(IContactReadRepository contactReadRepository)
    {
        _contactReadRepository = contactReadRepository;
    }

    public async Task<Result<IReadOnlyList<ContactDto>, IDomainError>> Handle(
        GetContactsForUserQuery request,
        CancellationToken cancellationToken)
    {
        var contacts = await _contactReadRepository.GetForUserAsync(
            request.UserId,
            request.Status,
            cancellationToken);

        var contactDtos = contacts
            .Select(x => new ContactDto(
                x.Id.Value,
                x.UserId1,
                x.UserId2,
                x.IsBlocked,
                x.BlockedBy,
                x.CreatedAtUtc.UtcDateTime,
                x.LastModifiedAtUtc.UtcDateTime))
            .ToList();

        return Result.Success<IReadOnlyList<ContactDto>, IDomainError>(contactDtos);
    }
}
