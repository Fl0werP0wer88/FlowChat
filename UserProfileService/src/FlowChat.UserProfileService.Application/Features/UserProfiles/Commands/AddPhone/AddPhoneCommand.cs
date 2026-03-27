using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddPhone;

public sealed record AddPhoneCommand(Guid UserId, string? Number) : ICommand<Guid>;

