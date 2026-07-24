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
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.Common.Eventing;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using Microsoft.Extensions.DependencyInjection;

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
            IAggregateBeforeSaveProcessorV2<CreateDuetConversationCommand, DuetConversationAggregate>,
            DuetConversationMembershipProjectionProcessor<CreateDuetConversationCommand>>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<CreateDuetConversationCommand, DuetConversationAggregate>,
            DuetConversationContactStateProjectionProcessor<CreateDuetConversationCommand>>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<BlockConversationParticipantCommand, DuetConversationAggregate>,
            DuetConversationContactStateProjectionProcessor<BlockConversationParticipantCommand>>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<UnblockConversationParticipantCommand, DuetConversationAggregate>,
            DuetConversationContactStateProjectionProcessor<UnblockConversationParticipantCommand>>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<CreateGroupConversationCommandV2, ConversationV2>,
            CreateGroupConversationMetadataProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<CreateDuetConversationCommandV2, ConversationV2>,
            CreateDuetConversationMetadataProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<CreateGroupFromDuetCommandV2, ConversationV2>,
            CreateGroupFromDuetConversationMetadataProcessorV2>();

        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<ConversationParticipantsAddedDomainEventV2, ConversationParticipant>,
            ConversationParticipantsAddedProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<ConversationParticipantsRemovedDomainEventV2, ConversationParticipant>,
            ConversationParticipantsRemovedProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveDeltaProcessorV2<
                ConversationParticipantsAddedDomainEventV2,
                ConversationParticipant>,
            AddConversationMembershipDeltaProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveDeltaProcessorV2<
                ConversationParticipantsRemovedDomainEventV2,
                ConversationParticipant>,
            RemoveConversationMembershipDeltaProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<MarkConversationAsReadCommandV2, ConversationParticipant>,
            MarkConversationAsReadParticipantProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<BlockConversationParticipantCommandV2, ConversationParticipant>,
            BlockConversationParticipantProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<UnblockConversationParticipantCommandV2, ConversationParticipant>,
            UnblockConversationParticipantProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<MuteConversationParticipantCommandV2, ConversationParticipant>,
            MuteConversationParticipantProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<UnmuteConversationParticipantCommandV2, ConversationParticipant>,
            UnmuteConversationParticipantProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<HideConversationParticipantCommandV2, ConversationParticipant>,
            HideConversationParticipantProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<UnhideConversationParticipantCommandV2, ConversationParticipant>,
            UnhideConversationParticipantProcessorV2>();
        services.AddScoped<
            IAggregateBeforeSaveProcessorV2<ChatMessageSentDomainEventV2, ConversationParticipant>,
            ChatMessageSentParticipantProcessorV2>();

        return services;
    }
}

