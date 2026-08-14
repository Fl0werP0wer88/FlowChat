using FlowChat.ChatService.Application;
using FlowChat.ChatService.Application.Features.Conversation.Processors;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FlowChat.ChatService.Domain.Entities.Conversation;

namespace FlowChat.ChatService.IntegrationTests.Application;

public sealed class ApplicationServiceRegistrationTests
{
    [Fact]
    public void AddApiApplicationServices_ForParticipantsAddedV2_RegistersParticipantAndDeltaProcessors()
    {
        using var serviceProvider = CreateServiceProvider();

        var participantProcessor = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessorV2<ConversationParticipantsAddedDomainEventV2, ConversationParticipant>>()
            .Should()
            .ContainSingle()
            .Subject;
        var deltaProcessor = serviceProvider
            .GetServices<IAggregateBeforeSaveDeltaProcessorV2<ConversationParticipantsAddedDomainEventV2, ConversationParticipant>>()
            .Should()
            .ContainSingle()
            .Subject;

        participantProcessor.Should().BeOfType<ConversationParticipantsAddedProcessorV2>();
        deltaProcessor.Should().BeOfType<AddConversationMembershipDeltaProcessorV2>();
    }

    [Fact]
    public void AddApiApplicationServices_ForParticipantsRemovedV2_RegistersParticipantAndDeltaProcessors()
    {
        using var serviceProvider = CreateServiceProvider();

        var participantProcessor = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessorV2<ConversationParticipantsRemovedDomainEventV2, ConversationParticipant>>()
            .Should()
            .ContainSingle()
            .Subject;
        var deltaProcessor = serviceProvider
            .GetServices<IAggregateBeforeSaveDeltaProcessorV2<ConversationParticipantsRemovedDomainEventV2, ConversationParticipant>>()
            .Should()
            .ContainSingle()
            .Subject;

        participantProcessor.Should().BeOfType<ConversationParticipantsRemovedProcessorV2>();
        deltaProcessor.Should().BeOfType<RemoveConversationMembershipDeltaProcessorV2>();
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
