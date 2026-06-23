using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetMainPhone;

public sealed record SetMainPhoneCommand(Guid UserId, Guid PhoneId) : ICommand<Guid>;

