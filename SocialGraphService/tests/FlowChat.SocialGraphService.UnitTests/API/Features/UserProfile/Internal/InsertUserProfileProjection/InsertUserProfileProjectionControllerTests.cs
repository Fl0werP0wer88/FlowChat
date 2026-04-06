using AutoFixture;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.InsertUserProfileProjection;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class InsertUserProfileProjectionControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Insert_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var repositoryMock = new Mock<IUserProfileProjectionRepository>();
        var controller = CreateController("expected-key", repositoryMock, new Mock<IUnitOfWork>());

        var result = await controller.Insert(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                UserName = "jdoe",
                DisplayName = "John Doe"
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Insert_WhenApiKeyMatches_InsertsProjectionAndSavesChanges()
    {
        UserProfileProjection? capturedProjection = null;
        var repositoryMock = new Mock<IUserProfileProjectionRepository>();
        repositoryMock
            .Setup(x => x.InsertAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfileProjection, CancellationToken>((projection, _) => capturedProjection = projection)
            .ReturnsAsync(true);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var controller = CreateController("expected-key", repositoryMock, unitOfWorkMock, "expected-key");

        var result = await controller.Insert(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                UserName = " jdoe ",
                DisplayName = " John Doe ",
                MainEmail = " john@example.com "
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedProjection.Should().NotBeNull();
        capturedProjection!.UserName.Should().Be("jdoe");
        capturedProjection.DisplayName.Should().Be("John Doe");
        capturedProjection.MainEmail.Should().Be("john@example.com");
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Insert_WhenProjectionAlreadyExists_ReturnsConflict()
    {
        var repositoryMock = new Mock<IUserProfileProjectionRepository>();
        repositoryMock
            .Setup(x => x.InsertAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var controller = CreateController("expected-key", repositoryMock, unitOfWorkMock, "expected-key");

        var result = await controller.Insert(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                UserName = "jdoe",
                DisplayName = "John Doe"
            },
            CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Insert_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var controller = CreateController("expected-key", new Mock<IUserProfileProjectionRepository>(), new Mock<IUnitOfWork>(), "expected-key");

        var result = await controller.Insert(
            new UserProfileProjectionRequest
            {
                UserProfileId = Guid.Empty,
                UserName = "jdoe",
                DisplayName = "John Doe"
            },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static InsertUserProfileProjectionController CreateController(
        string expectedApiKey,
        Mock<IUserProfileProjectionRepository> repositoryMock,
        Mock<IUnitOfWork> unitOfWorkMock,
        string? providedApiKey = null)
    {
        var controller = new InsertUserProfileProjectionController(
            repositoryMock.Object,
            unitOfWorkMock.Object,
            InternalUserProfileProjectionControllerTestFactory.CreateApiSettingsManager(expectedApiKey).Object);

        InternalUserProfileProjectionControllerTestFactory.ConfigureControllerContext(controller, providedApiKey);
        return controller;
    }
}
