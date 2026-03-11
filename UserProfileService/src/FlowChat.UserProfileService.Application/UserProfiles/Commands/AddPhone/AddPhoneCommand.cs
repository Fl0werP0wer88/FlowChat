using FlowChat.Application.Abstractions;

namespace FlowChat.UserProfileService.Application.UserProfiles.Commands.AddPhone;

public sealed record AddPhoneCommand(Guid UserId, string? Number) : ICommand<Guid>;
