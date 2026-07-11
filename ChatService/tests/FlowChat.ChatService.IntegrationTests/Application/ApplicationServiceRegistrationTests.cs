using FlowChat.ChatService.Application;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;
using FlowChat.ChatService.Application.Features.Conversation.Processors;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.IntegrationTests.Application;

public sealed class ApplicationServiceRegistrationTests
{
    [Fact]
    public void AddApiApplicationServices_RegistersDuetConversationProjectionProcessor()
    {
        var services = new ServiceCollection();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        services.AddLogging();
        services.AddSingleton(publisherMock.Object);
        services.AddApiApplicationServices();

        using var serviceProvider = services.BuildServiceProvider();

        var processor = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessor<CreateDuetConversationCommand, DuetConversationAggregate>>()
            .Should()
            .ContainSingle()
            .Subject;

        processor.Should().BeOfType<DuetConversationProjectionProcessor<CreateDuetConversationCommand>>();
    }
}
