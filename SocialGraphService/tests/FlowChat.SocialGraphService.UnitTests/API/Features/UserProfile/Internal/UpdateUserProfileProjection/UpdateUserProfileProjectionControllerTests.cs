using AutoFixture;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UpdateUserProfileProjection;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UpdateUserProfileProjectionControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Update_WhenApiKeyMatches_UpdatesProjectionAndSavesChanges()
    {
        UserProfileProjection? capturedProjection = null;
        var repositoryMock = new Mock<IUserProfileProjectionRepository>();
        repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfileProjection, CancellationToken>((projection, _) => capturedProjection = projection)
            .ReturnsAsync(true);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var controller = CreateController("expected-key", repositoryMock, unitOfWorkMock, "expected-key");

        var result = await controller.Update(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                UserName = " jane.doe ",
                DisplayName = " Jane Doe ",
                Bio = " updated "
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedProjection.Should().NotBeNull();
        capturedProjection!.UserName.Should().Be("jane.doe");
        capturedProjection.DisplayName.Should().Be("Jane Doe");
        capturedProjection.Bio.Should().Be("updated");
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenProjectionDoesNotExist_ReturnsNotFound()
    {
        var repositoryMock = new Mock<IUserProfileProjectionRepository>();
        repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var controller = CreateController("expected-key", repositoryMock, unitOfWorkMock, "expected-key");

        var result = await controller.Update(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                UserName = "jane.doe",
                DisplayName = "Jane Doe"
            },
            CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var controller = CreateController("expected-key", new Mock<IUserProfileProjectionRepository>(), new Mock<IUnitOfWork>(), "expected-key");

        var result = await controller.Update(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                UserName = string.Empty,
                DisplayName = "Jane Doe"
            },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static UpdateUserProfileProjectionController CreateController(
        string expectedApiKey,
        Mock<IUserProfileProjectionRepository> repositoryMock,
        Mock<IUnitOfWork> unitOfWorkMock,
        string? providedApiKey = null)
    {
        var controller = new UpdateUserProfileProjectionController(
            repositoryMock.Object,
            unitOfWorkMock.Object,
            InternalUserProfileProjectionControllerTestFactory.CreateApiSettingsManager(expectedApiKey).Object);

        InternalUserProfileProjectionControllerTestFactory.ConfigureControllerContext(controller, providedApiKey);
        return controller;
    }
}
