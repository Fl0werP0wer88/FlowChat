using CSharpFunctionalExtensions;
using FlowChat.Domain.Abstractions;
using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Consumers.Kafka;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserCreatedSubscriberTests
{
    [Fact]
    public async Task HandleAsync_MapsEmailAndPhoneToCreateInitialUserProfileCommand()
    {
        var userId = Guid.NewGuid();
        var mediator = new CapturingMediator(Result.Success<Guid, IDomainError>(userId));
        var subscriber = new UserCreatedSubscriber(mediator, NullLogger<UserCreatedSubscriber>.Instance);
        var message = new UserCreatedIntegrationEvent
        {
            UserId = userId,
            UserName = "jdoe",
            DisplayName = "John Doe",
            Email = "john@example.com",
            PhoneNumber = "+48123123123"
        };

        await subscriber.HandleAsync(message, CancellationToken.None);

        var command = Assert.IsType<CreateInitialUserProfileCommand>(mediator.SentRequest);
        Assert.Equal("jdoe", command.UserName);
        Assert.Equal("John Doe", command.DisplayName);
        Assert.Equal("john@example.com", command.Email);
        Assert.Equal("+48123123123", command.Phone);
        Assert.Equal(userId, command.UserId);
    }

    private sealed class CapturingMediator(object response) : IMediator
    {
        public object? SentRequest { get; private set; }

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            SentRequest = request;
            return Task.FromResult((object?)response);
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SentRequest = request;
            return Task.FromResult((TResponse)response);
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            SentRequest = request;
            return EmptyAsyncEnumerable<object?>();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            SentRequest = request;
            return EmptyAsyncEnumerable<TResponse>();
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            SentRequest = request;
            return Task.CompletedTask;
        }

        private static async IAsyncEnumerable<T> EmptyAsyncEnumerable<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
