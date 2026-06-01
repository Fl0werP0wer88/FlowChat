using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.UserProfileService.Application;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddEmail;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddPhone;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.ConfirmEmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.DeleteProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SendEmailVerification;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetAuthEmail;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetMainEmail;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetMainPhone;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.UpdateProfile;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using Microsoft.Extensions.DependencyInjection;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.IntegrationTests;

public sealed class ApplicationServiceRegistrationTests
{
    [Fact]
    public void AddApiApplicationServices_RegistersUserProfileProjectionBeforeSaveProcessors()
    {
        var services = new ServiceCollection();
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        services.AddLogging();
        services.AddSingleton(publisherMock.Object);
        services.AddApiApplicationServices();

        using var serviceProvider = services.BuildServiceProvider();

        AssertUserProfileProjectionProcessorRegistered<CreateInitialUserProfileCommand>(serviceProvider);
        AssertUserProfileProjectionProcessorRegistered<AddEmailCommand>(serviceProvider);
        AssertUserProfileProjectionProcessorRegistered<AddPhoneCommand>(serviceProvider);
        AssertUserProfileProjectionProcessorRegistered<ConfirmEmailVerificationCommand>(serviceProvider);
        AssertUserProfileProjectionProcessorRegistered<SetAuthEmailCommand>(serviceProvider);
        AssertUserProfileProjectionProcessorRegistered<SetMainEmailCommand>(serviceProvider);
        AssertUserProfileProjectionProcessorRegistered<SetMainPhoneCommand>(serviceProvider);
        AssertUserProfileProjectionProcessorRegistered<UpdateProfileCommand>(serviceProvider);
        AssertUserProfileProjectionProcessorRegistered<DeleteProfileCommand>(serviceProvider);

        var emailVerificationProcessors = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessor<SendEmailVerificationCommand, EmailVerificationProcess>>();
        emailVerificationProcessors.Should().BeEmpty();
    }

    private static void AssertUserProfileProjectionProcessorRegistered<TCommand>(IServiceProvider serviceProvider)
    {
        var processors = serviceProvider
            .GetServices<IAggregateBeforeSaveProcessor<TCommand, DomainUserProfile>>()
            .Should()
            .ContainSingle()
            .Subject;

        processors.Should().BeOfType<
            PublishProjectionIntegrationEventProcessor<TCommand, DomainUserProfile, UserProfileReadModel>>();
    }
}
