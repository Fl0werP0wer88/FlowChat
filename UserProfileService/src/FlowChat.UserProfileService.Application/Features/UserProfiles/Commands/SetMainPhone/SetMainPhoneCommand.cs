using FlowChat.Application.Abstractions;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SetMainPhone;

public sealed record SetMainPhoneCommand(Guid UserId, Guid PhoneId) : ICommand<Guid>;
