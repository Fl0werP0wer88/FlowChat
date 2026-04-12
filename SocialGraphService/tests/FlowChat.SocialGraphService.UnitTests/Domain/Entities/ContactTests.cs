using FlowChat.SocialGraphService.Domain.Entities.Contact;
using FlowChat.SocialGraphService.Domain.Entities.Contact.Events;
using FluentAssertions;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class ContactTests
{
    [Fact]
    public void Create_WhenOwnerUserIdIsEmpty_ThrowsArgumentOutOfRangeException()
    {
        var act = () => Contact.Create(Guid.Empty, Guid.NewGuid(), "John Doe");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WhenContactUserIdIsEmpty_ThrowsArgumentOutOfRangeException()
    {
        var act = () => Contact.Create(Guid.NewGuid(), Guid.Empty, "John Doe");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WhenOwnerAndContactUsersAreSame_ThrowsArgumentException()
    {
        var userId = Guid.NewGuid();

        var act = () => Contact.Create(userId, userId, "John Doe");

        act.Should().Throw<ArgumentException>()
            .WithMessage("OwnerUserId and ContactUserId must be different.");
    }

    [Fact]
    public void Create_WhenContactIsCreated_RaisesContactAddedDomainEvent()
    {
        var contact = Contact.Create(Guid.NewGuid(), Guid.NewGuid(), "John Doe");

        contact.DomainEvents.OfType<ContactAddedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void MarkDeleted_WhenCalled_RaisesContactDeletedDomainEvent()
    {
        var contact = Contact.Create(Guid.NewGuid(), Guid.NewGuid(), "John Doe");
        contact.PopDomainEvents();

        contact.MarkDeleted();

        contact.DomainEvents.OfType<ContactDeletedDomainEvent>().Should().ContainSingle();
    }
}
