using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
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

    [Fact]
    public void Constructor_NewAggregateRoot_IsNotDeleted()
    {
        var aggregate = TestAggregate.Create();

        aggregate.DeletedAt.Should().BeNull();
        aggregate.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Delete_WhenAggregateRootIsNotDeleted_SetsDeletedAt()
    {
        var deletedAt = UtcDateTimeOffset.Create(new DateTimeOffset(2026, 6, 3, 12, 30, 0, TimeSpan.Zero));
        var aggregate = TestAggregate.Create();

        aggregate.Delete(deletedAt);

        aggregate.DeletedAt.Should().Be(deletedAt);
        aggregate.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_WhenAggregateRootIsAlreadyDeleted_DoesNotOverwriteDeletedAt()
    {
        var initialDeletedAt = UtcDateTimeOffset.Create(new DateTimeOffset(2026, 6, 3, 12, 30, 0, TimeSpan.Zero));
        var laterDeletedAt = initialDeletedAt.AddHours(1);
        var aggregate = TestAggregate.Create();

        aggregate.Delete(initialDeletedAt);
        aggregate.Delete(laterDeletedAt);

        aggregate.DeletedAt.Should().Be(initialDeletedAt);
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
