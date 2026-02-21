using MediatR;

namespace FlowChat.UserProfileService.Application.UserProfiles.Queries;

public sealed record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto?>;
