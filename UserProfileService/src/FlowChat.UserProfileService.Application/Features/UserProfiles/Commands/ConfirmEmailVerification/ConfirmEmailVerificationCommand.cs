using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.ConfirmEmailVerification;

public sealed record ConfirmEmailVerificationCommand(string Token) : ICommand<Unit>;
