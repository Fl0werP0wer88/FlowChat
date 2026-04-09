using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.AddPhone;

public sealed record AddPhoneRequest(string? Number) : IServiceInput;
