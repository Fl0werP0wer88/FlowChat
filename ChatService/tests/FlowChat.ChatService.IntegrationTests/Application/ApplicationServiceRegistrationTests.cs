using FlowChat.ChatService.Application;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;
using FlowChat.ChatService.Application.Features.Conversation.Commands.BlockConversationParticipant;
using FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;
using FlowChat.ChatService.Application.Features.Conversation.Commands.UnblockConversationParticipant;
using FlowChat.ChatService.Application.Features.Conversation.Processors;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;
using FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;
using FlowChat.ChatService.Domain.Entities.Conversation;
using GroupConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;

namespace FlowChat.ChatService.IntegrationTests.Application;

public sealed class ApplicationServiceRegistrationTests
{
    [Fact]
    public void AddApiApplicationServices_ForCreateDuetConversation_RegistersBothProjectionProcessors()
    {
        var services = new ServiceCollection();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        services.AddLogging();
        services.AddSingleton(publisherMock.Object);
        services.AddApiApplicationServices();

        using var serviceProvider = services.BuildServiceProvider();

        var processors = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessorV2<CreateDuetConversationCommand, DuetConversationAggregate>>()
            .Should()
            .HaveCount(2)
            .And.Subject;

        processors.Should().ContainSingle(processor => processor is DuetConversationMembershipProjectionProcessor<CreateDuetConversationCommand>);
        processors.Should().ContainSingle(processor => processor is DuetConversationContactStateProjectionProcessor<CreateDuetConversationCommand>);
    }

    [Fact]
    public void AddApiApplicationServices_ForBlockConversationParticipant_RegistersOnlyContactStateProjectionProcessor()
    {
        using var serviceProvider = CreateServiceProvider();

        var processor = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessorV2<BlockConversationParticipantCommand, DuetConversationAggregate>>()
            .Should()
            .ContainSingle()
            .Subject;

        processor.Should().BeOfType<DuetConversationContactStateProjectionProcessor<BlockConversationParticipantCommand>>();
    }

    [Fact]
    public void AddApiApplicationServices_ForUnblockConversationParticipant_RegistersOnlyContactStateProjectionProcessor()
    {
        using var serviceProvider = CreateServiceProvider();

        var processor = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessorV2<UnblockConversationParticipantCommand, DuetConversationAggregate>>()
            .Should()
            .ContainSingle()
            .Subject;

        processor.Should().BeOfType<DuetConversationContactStateProjectionProcessor<UnblockConversationParticipantCommand>>();
    }

    [Fact]
    public void AddApiApplicationServices_ForAddParticipantsV2_RegistersSingleMembershipDeltaProcessor()
    {
        using var serviceProvider = CreateServiceProvider();

        var processor = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessorV2<AddGroupParticipantsCommandV2, ConversationMembership>>()
            .Should()
            .ContainSingle()
            .Subject;

        processor.Should().BeOfType<AddConversationMembershipDeltaProcessorV2>();
    }

    [Fact]
    public void AddApiApplicationServices_ForConversationCreatedV2_RegistersSingleMembershipDeltaProcessor()
    {
        using var serviceProvider = CreateServiceProvider();

        var processor = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessorV2<ConversationCreatedDomainEventV2, ConversationMembership>>()
            .Should()
            .ContainSingle()
            .Subject;

        processor.Should().BeOfType<CreateConversationMembershipDeltaProcessorV2>();
    }

    [Fact]
    public void AddApiApplicationServices_ForRemoveParticipantsV2_RegistersSingleMembershipDeltaProcessor()
    {
        using var serviceProvider = CreateServiceProvider();

        var processor = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessorV2<RemoveGroupParticipantsCommandV2, ConversationMembership>>()
            .Should()
            .ContainSingle()
            .Subject;

        processor.Should().BeOfType<RemoveConversationMembershipDeltaProcessorV2>();
    }

    [Fact]
    public void AddApiApplicationServices_ForLegacyGroupConversation_DoesNotRegisterDeltaProcessor()
    {
        using var serviceProvider = CreateServiceProvider();

        var processors = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessorV2<CreateGroupConversationCommand, GroupConversationAggregate>>();

        processors.Should().BeEmpty();
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        services.AddLogging();
        services.AddSingleton(publisherMock.Object);
        services.AddApiApplicationServices();

        return services.BuildServiceProvider();
    }
}
