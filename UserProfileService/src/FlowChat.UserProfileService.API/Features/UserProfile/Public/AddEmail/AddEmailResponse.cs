using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.AddEmail;

public sealed record AddEmailResponse(Guid EmailId) : IServiceOutput;
