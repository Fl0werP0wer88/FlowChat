using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.UnitTests;

public class UnitTest1
{
    [Fact]
    public void UserProfile_Create_WithTypedId_AssignsTypedAggregateId()
    {
        var id = Id<UserProfile>.New();

        var profile = UserProfile.Create("jdoe", "John Doe", id: id);

        Assert.Equal(id, profile.Id);
        Assert.Equal(id.Value, profile.Id.Value);
    }

    [Fact]
    public void UserProfile_Rehydrate_DoesNotEmitDomainEvents()
    {
        var profile = UserProfile.Rehydrate("jdoe", "John Doe", id: Id<UserProfile>.New());

        Assert.Empty(profile.DomainEvents);
    }

    [Fact]
    public void UserProfile_AddEmail_AddsEmailToAggregate()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());

        var email = profile.AddEmail("john@example.com");

        Assert.Single(profile.Emails);
        Assert.Equal(email.Id, profile.Emails[0].Id);
        Assert.Equal("john@example.com", profile.Emails[0].Address);
        Assert.Equal(profile.Id.Value, profile.Emails[0].UserProfileId);
        Assert.True(profile.Emails[0].IsMain);
    }

    [Fact]
    public void UserProfile_AddEmail_WithDuplicateAddress_Throws()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());
        profile.AddEmail("john@example.com");

        Assert.Throws<InvalidOperationException>(() => profile.AddEmail("JOHN@example.com"));
    }

    [Fact]
    public void UserProfile_AddEmail_SecondEmail_IsNotMain()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());
        profile.AddEmail("john@example.com");

        var secondEmail = profile.AddEmail("john.secondary@example.com");

        Assert.False(secondEmail.IsMain);
    }

    [Fact]
    public void UserProfile_SetMainEmail_SwitchesMainFlag()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());
        var firstEmail = profile.AddEmail("john@example.com");
        var secondEmail = profile.AddEmail("john.secondary@example.com");

        profile.SetMainEmail(secondEmail.Id.Value);

        Assert.False(firstEmail.IsMain);
        Assert.True(secondEmail.IsMain);
        Assert.Single(profile.Emails, x => x.IsMain);
        var @event = Assert.IsType<MainEmailChangedDomainEvent>(Assert.Single(profile.DomainEvents));
        Assert.Equal(profile.Id.Value, @event.AggregateId);
        Assert.Equal(secondEmail.Id.Value, @event.EmailId);
        Assert.Equal(secondEmail.Address, @event.Address);
    }

    [Fact]
    public void UserProfile_SetMainEmail_WhenEmailDoesNotExist_Throws()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());
        profile.AddEmail("john@example.com");

        Assert.Throws<InvalidOperationException>(() => profile.SetMainEmail(Guid.NewGuid()));
    }

    [Fact]
    public void UserProfile_SetMainEmail_WhenEmailIsAlreadyMain_DoesNotEmitDomainEvent()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());
        var email = profile.AddEmail("john@example.com");

        profile.SetMainEmail(email.Id.Value);

        Assert.Empty(profile.DomainEvents);
    }

    [Fact]
    public void UserProfile_AddPhone_AddsPhoneToAggregate()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());

        var phone = profile.AddPhone("+48123123123");

        Assert.Single(profile.Phones);
        Assert.Equal(phone.Id, profile.Phones[0].Id);
        Assert.Equal("+48123123123", profile.Phones[0].Number);
        Assert.Equal(profile.Id.Value, profile.Phones[0].UserProfileId);
        Assert.True(profile.Phones[0].IsMain);
    }

    [Fact]
    public void UserProfile_AddPhone_WithDuplicateNumber_Throws()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());
        profile.AddPhone("+48123123123");

        Assert.Throws<InvalidOperationException>(() => profile.AddPhone("+48123123123"));
    }

    [Fact]
    public void UserProfile_AddPhone_SecondPhone_IsNotMain()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());
        profile.AddPhone("+48123123123");

        var secondPhone = profile.AddPhone("+48987654321");

        Assert.False(secondPhone.IsMain);
    }

    [Fact]
    public void UserProfile_SetMainPhone_SwitchesMainFlag()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());
        var firstPhone = profile.AddPhone("+48123123123");
        var secondPhone = profile.AddPhone("+48987654321");

        profile.SetMainPhone(secondPhone.Id.Value);

        Assert.False(firstPhone.IsMain);
        Assert.True(secondPhone.IsMain);
        Assert.Single(profile.Phones, x => x.IsMain);
        var @event = Assert.IsType<MainPhoneChangedDomainEvent>(Assert.Single(profile.DomainEvents));
        Assert.Equal(profile.Id.Value, @event.AggregateId);
        Assert.Equal(secondPhone.Id.Value, @event.PhoneId);
        Assert.Equal(secondPhone.Number, @event.Number);
    }

    [Fact]
    public void UserProfile_SetMainPhone_WhenPhoneDoesNotExist_Throws()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());
        profile.AddPhone("+48123123123");

        Assert.Throws<InvalidOperationException>(() => profile.SetMainPhone(Guid.NewGuid()));
    }

    [Fact]
    public void UserProfile_SetMainPhone_WhenPhoneIsAlreadyMain_DoesNotEmitDomainEvent()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", id: Id<UserProfile>.New());
        var phone = profile.AddPhone("+48123123123");

        profile.SetMainPhone(phone.Id.Value);

        Assert.Empty(profile.DomainEvents);
    }
}
