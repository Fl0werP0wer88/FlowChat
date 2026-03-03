using FlowChat.SocialGraphService.Application.Contracts;
using FlowChat.SocialGraphService.Application.SocialGraphs;

namespace FlowChat.SocialGraphService.Application.SocialGraphs.Commands.CreateDefaultSocialGraph;

public sealed record CreateDefaultSocialGraphCommand(
    Guid UserId,
    string Login,
    string? FirstName = null,
    string? LastName = null,
    string? Email = null) : ICommand<SocialGraphDto>;
