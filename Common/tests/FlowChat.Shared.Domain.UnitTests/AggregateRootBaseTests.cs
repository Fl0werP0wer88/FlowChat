using FlowChat.Shared.Domain;
using FluentAssertions;

namespace FlowChat.Shared.Domain.UnitTests;

public sealed class AggregateRootBaseTests
{
    [Fact]
    public void Constructor_NewAggregateRoot_SetsInitialVersion()
    {
        var aggregate = TestAggregate.Create();

        aggregate.Version.Should().Be(1);
    }

    [Fact]
    public void IncrementVersion_WhenCalled_IncrementsVersion()
    {
        var aggregate = TestAggregate.Create();

        aggregate.IncrementVersion();

        aggregate.Version.Should().Be(2);
    }

    private sealed class TestAggregate : AggregateRootBase<TestAggregate>
    {
        private TestAggregate()
            : base(Id<TestAggregate>.New())
        {
        }

        public static TestAggregate Create() => new();
    }
}
