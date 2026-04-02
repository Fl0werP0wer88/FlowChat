using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddPhone;

public sealed record AddPhoneCommand(Guid UserId, string? Number) : ICommand<Guid>;

