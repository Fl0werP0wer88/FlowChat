using FlowChat.ChatService.Application.Features.Conversation.Commands.BlockConversationParticipant;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;
using FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;
using FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;
using FlowChat.ChatService.Application.Features.Conversation.Commands.UnblockConversationParticipant;
using FlowChat.ChatService.Application.Features.Conversation.Processors;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.Common.Eventing;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using GroupConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;

namespace FlowChat.ChatService.Application;

public static class ApiApplicationServiceRegistration
{
    public static IServiceCollection AddApiApplicationServices(this IServiceCollection services)
        => services.AddCommonApplicationServices();
}

public static class ConsumerApplicationServiceRegistration
{
    public static IServiceCollection AddConsumerApplicationServices(this IServiceCollection services)
        => services.AddCommonApplicationServices();
}

internal static class CommonApplicationServiceRegistration
{
    public static IServiceCollection AddCommonApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(CommonApplicationServiceRegistration).Assembly;

        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, AppDomain.CurrentDomain.GetAssemblies());
        services.AddFlowChatValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
            cfg.AddFlowChatBehaviors();
        });
        services.AddScoped<ILocalEventDispatcher, LocalEventDispatcher>();
        services.AddScoped<
            IDeltaProjectionRevisionProvider<GroupConversationAggregate, GroupConversationMembershipReadModel>,
            GroupConversationMembershipRevisionProvider>();
        services.AddScoped<
            IAggregateBeforeSaveProcessor<CreateDuetConversationCommand, DuetConversationAggregate>,
            DuetConversationMembershipProjectionProcessor<CreateDuetConversationCommand>>();
        services.AddScoped<
            IAggregateBeforeSaveProcessor<CreateDuetConversationCommand, DuetConversationAggregate>,
            DuetConversationContactStateProjectionProcessor<CreateDuetConversationCommand>>();
        services.AddScoped<
            IAggregateBeforeSaveProcessor<BlockConversationParticipantCommand, DuetConversationAggregate>,
            DuetConversationContactStateProjectionProcessor<BlockConversationParticipantCommand>>();
        services.AddScoped<
            IAggregateBeforeSaveProcessor<UnblockConversationParticipantCommand, DuetConversationAggregate>,
            DuetConversationContactStateProjectionProcessor<UnblockConversationParticipantCommand>>();
        services.AddScoped<
            IAggregateBeforeSaveProcessor<CreateGroupConversationCommand, GroupConversationAggregate>,
            GroupConversationMembershipProjectionProcessor<CreateGroupConversationCommand>>();
        services.AddScoped<
            IAggregateBeforeSaveProcessor<CreateGroupFromDuetCommand, GroupConversationAggregate>,
            GroupConversationMembershipProjectionProcessor<CreateGroupFromDuetCommand>>();
        services.AddScoped<
            IAggregateBeforeSaveProcessor<AddGroupParticipantsCommand, GroupConversationAggregate>,
            GroupConversationMembershipProjectionProcessor<AddGroupParticipantsCommand>>();
        services.AddScoped<
            IAggregateBeforeSaveProcessor<RemoveGroupParticipantsCommand, GroupConversationAggregate>,
            GroupConversationMembershipProjectionProcessor<RemoveGroupParticipantsCommand>>();

        return services;
    }
}

