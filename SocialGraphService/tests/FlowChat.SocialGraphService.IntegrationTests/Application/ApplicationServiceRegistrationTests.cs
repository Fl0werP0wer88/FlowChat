using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.SocialGraphService.Application;
using FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;
using FlowChat.SocialGraphService.Application.Features.Contact.Commands.DeleteContact;
using FlowChat.SocialGraphService.Application.Features.Contact.Processors;
using Microsoft.Extensions.DependencyInjection;
using ContactAggregate = FlowChat.SocialGraphService.Domain.Entities.Contact.Contact;

namespace FlowChat.SocialGraphService.IntegrationTests.Application;

public sealed class ApplicationServiceRegistrationTests
{
    [Fact]
    public void AddApiApplicationServices_RegistersContactProjectionBeforeSaveProcessors()
    {
        var services = new ServiceCollection();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        services.AddLogging();
        services.AddSingleton(publisherMock.Object);
        services.AddApiApplicationServices();

        using var serviceProvider = services.BuildServiceProvider();

        AssertContactProjectionProcessorRegistered<AddContactCommand>(serviceProvider);
        AssertContactProjectionProcessorRegistered<DeleteContactCommand>(serviceProvider);
    }

    private static void AssertContactProjectionProcessorRegistered<TCommand>(IServiceProvider serviceProvider)
    {
        var processor = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessor<TCommand, ContactAggregate>>()
            .Should()
            .ContainSingle()
            .Subject;

        processor.Should().BeOfType<ContactProjectionProcessor<TCommand>>();
    }
}
