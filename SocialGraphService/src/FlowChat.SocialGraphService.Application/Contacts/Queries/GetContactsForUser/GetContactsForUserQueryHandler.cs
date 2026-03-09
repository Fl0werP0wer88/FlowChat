using FlowChat.Application.Abstractions;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.Domain.Abstractions;

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
            cancellationToken);

        var contactDtos = contacts
            .Select(x => new ContactDto(
                x.Id.Value,
                x.OwnerUserId,
                x.ContactUserId,
                x.Login,
                x.FirstName,
                x.LastName,
                x.PhoneNumber,
                x.Email,
                x.IsBlocked,
                x.CreatedAtUtc.UtcDateTime,
                x.LastModifiedAtUtc.UtcDateTime))
            .ToList();

        return Result.Success<IReadOnlyList<ContactDto>, IDomainError>(contactDtos);
    }
}
