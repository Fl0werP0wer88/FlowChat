using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;

namespace FlowChat.Core.UnitTests.Results;

public sealed class FlowChatResultTests
{
    [Fact]
    public void Success_WhenCalled_ReturnsSuccessfulResult()
    {
        var result = FlowChatResult.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Failure_WhenCalled_ReturnsFailedResultWithError()
    {
        var error = new TestDomainError("Invalid request.");

        var result = FlowChatResult.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void FlowChatResult_WhenInspected_DoesNotExposeValue()
    {
        typeof(FlowChatResult).GetProperty("Value").Should().BeNull();
    }

    private sealed record TestDomainError(string? ErrorMessage) : IDomainError
    {
        public FailureKind FailureKind => FailureKind.None;

        public ErrorType ErrorType => ErrorType.BadRequest;

        public List<string>? Errors => [];
    }
}
