using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
using UserProfileMarker = FlowChat.SocialGraphService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IContactWriteRepository : IWriteRepository<Contact>
{
    Task<bool> ExistsAsync(
        Id<UserProfileMarker> ownerUserId,
        Id<UserProfileMarker> contactUserId,
        CancellationToken cancellationToken = default);

    Task<Contact?> GetByOwnerAndContactAsync(
        Id<UserProfileMarker> ownerUserId,
        Id<UserProfileMarker> contactUserId,
        CancellationToken cancellationToken = default);
}

