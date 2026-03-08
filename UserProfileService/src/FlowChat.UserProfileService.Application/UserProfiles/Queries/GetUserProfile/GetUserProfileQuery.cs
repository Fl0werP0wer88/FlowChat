using MediatR;

namespace FlowChat.UserProfileService.Application.UserProfiles.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto?>;
