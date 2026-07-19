using FlowChat.ChatService.Application.Features.Conversation.Commands.BlockConversationParticipant;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;
using FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;
using FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;
using FlowChat.ChatService.Application.Features.Conversation.Commands.UnblockConversationParticipant;
using FlowChat.ChatService.Application.Features.Conversation.Commands.MarkConversationAsRead;
using FlowChat.ChatService.Application.Features.Conversation.Commands.MuteConversationParticipant;
using FlowChat.ChatService.Application.Features.Conversation.Commands.UnmuteConversationParticipant;
using FlowChat.ChatService.Application.Features.Conversation.Commands.HideConversationParticipant;
using FlowChat.ChatService.Application.Features.Conversation.Commands.UnhideConversationParticipant;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
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

        services.AddScoped<ConversationMembershipDeltaPublisherV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessor<ConversationCreatedDomainEventV2, ConversationMembership>,
            CreateConversationMembershipDeltaProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessor<AddGroupParticipantsCommandV2, ConversationMembership>,
            AddConversationMembershipDeltaProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessor<RemoveGroupParticipantsCommandV2, ConversationMembership>,
            RemoveConversationMembershipDeltaProcessorV2>();

        services.AddConversationMetadataProcessor<CreateGroupConversationCommandV2>();
        services.AddConversationMetadataProcessor<CreateDuetConversationCommandV2>();
        services.AddConversationMetadataProcessor<CreateGroupFromDuetCommandV2>();

        services.AddConversationParticipantProcessor<ConversationParticipantAddedDomainEventV2>();
        services.AddConversationParticipantProcessor<ConversationParticipantRemovedDomainEventV2>();
        services.AddConversationParticipantProcessor<MarkConversationAsReadCommandV2>();
        services.AddConversationParticipantProcessor<BlockConversationParticipantCommandV2>();
        services.AddConversationParticipantProcessor<UnblockConversationParticipantCommandV2>();
        services.AddConversationParticipantProcessor<MuteConversationParticipantCommandV2>();
        services.AddConversationParticipantProcessor<UnmuteConversationParticipantCommandV2>();
        services.AddConversationParticipantProcessor<HideConversationParticipantCommandV2>();
        services.AddConversationParticipantProcessor<UnhideConversationParticipantCommandV2>();
        services.AddConversationParticipantProcessor<ChatMessageSentDomainEventV2>();

        return services;
    }

    private static IServiceCollection AddConversationMetadataProcessor<TTrigger>(
        this IServiceCollection services)
    {
        services.AddScoped<
            IAggregateBeforeSaveProcessor<TTrigger, ConversationV2>,
            ConversationMetadataProcessorV2<TTrigger>>();
        return services;
    }

    private static IServiceCollection AddConversationParticipantProcessor<TTrigger>(
        this IServiceCollection services)
    {
        services.AddScoped<
            IAggregateBeforeSaveProcessor<TTrigger, ConversationParticipant>,
            ConversationParticipantProcessorV2<TTrigger>>();
        return services;
    }
}

