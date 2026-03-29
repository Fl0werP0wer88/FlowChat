using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.UnitTests;

public class UnitTest1
{
    [Fact]
    public void UserProfile_Create_WithTypedId_AssignsTypedAggregateId()
    {
        var id = Id<UserProfile>.New();

        var profile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create("john@example.com"), id: id);

        Assert.Equal(id, profile.Id);
        Assert.Equal(id.Value, profile.Id.Value);
        Assert.Single(profile.DomainEvents.OfType<UserProfileCreatedDomainEvent>());
        Assert.Single(profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
    }

    [Fact]
    public void Email_Create_WithInvalidAddress_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => EmailAddress.Create("not-an-email"));

        Assert.StartsWith(EmailAddress.InvalidEmailAddressMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Email_Create_NormalizesTrimmedAddress()
    {
        var userProfileId = Id<UserProfile>.New();

        var email = Email.Create(userProfileId, EmailAddress.Create(" john@example.com "));

        Assert.Equal("john@example.com", email.Address.Value);
        Assert.False(email.IsConfirmed);
    }

    [Fact]
    public void UserProfile_AddEmail_AddsSecondaryEmailToAggregate()
    {
        var profile = CreateExistingProfile();
        var existingMainEmail = Assert.Single(profile.Emails);

        var email = profile.AddEmail("john.secondary@example.com");

        Assert.Equal(2, profile.Emails.Count);
        Assert.Equal(email.Id, profile.Emails.Single(x => x.Address.Value == "john.secondary@example.com").Id);
        Assert.Equal("john.secondary@example.com", email.Address.Value);
        Assert.Equal(profile.Id, email.UserProfileId);
        Assert.False(email.IsMain);
        Assert.False(email.IsAuth);
        Assert.False(email.IsConfirmed);
        Assert.True(existingMainEmail.IsMain);
        var emailAddedEvent = Assert.Single(profile.DomainEvents.OfType<EmailAddedDomainEvent>());
        Assert.Equal(profile.Id, emailAddedEvent.UserProfileId);
        Assert.Equal(email.Id, emailAddedEvent.EmailId);
        Assert.Equal(email.Address, emailAddedEvent.Email);
        var @event = Assert.Single(profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
        Assert.Equal(existingMainEmail.Address.Value, @event.AggregateState.MainEmail);
        Assert.False(@event.AggregateState.IsMainEmailConfirmed);
        Assert.Null(@event.AggregateState.MainPhone);
    }

    [Fact]
    public void UserProfile_AddEmail_WithDuplicateAddress_Throws()
    {
        var profile = CreateExistingProfile();

        Assert.Throws<InvalidOperationException>(() => profile.AddEmail("JOHN@example.com"));
    }

    [Fact]
    public void UserProfile_SetMainEmail_SwitchesMainFlag()
    {
        var profile = CreateExistingProfile();
        var firstEmail = Assert.Single(profile.Emails);
        var secondEmail = profile.AddEmail("john.secondary@example.com");
        profile.ClearEvents();

        profile.SetMainEmail(secondEmail.Id);

        Assert.False(firstEmail.IsMain);
        Assert.True(secondEmail.IsMain);
        Assert.Single(profile.Emails, x => x.IsMain);
        var emailChangedEvent = Assert.Single(profile.DomainEvents.OfType<MainEmailChangedDomainEvent>());
        Assert.Equal(profile.Id.Value, emailChangedEvent.AggregateId);
        Assert.Equal(profile.Id, emailChangedEvent.UserProfileId);
        Assert.Equal(secondEmail.Id, emailChangedEvent.EmailId);
        Assert.Equal(secondEmail.Address, emailChangedEvent.Address);
        var stateChangedEvent = Assert.Single(profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
        Assert.Equal(secondEmail.Address.Value, stateChangedEvent.AggregateState.MainEmail);
        Assert.False(stateChangedEvent.AggregateState.IsMainEmailConfirmed);
    }

    [Fact]
    public void UserProfile_SetMainEmail_WhenEmailDoesNotExist_Throws()
    {
        var profile = CreateExistingProfile();

        Assert.Throws<InvalidOperationException>(() => profile.SetMainEmail(Id<Email>.New()));
    }

    [Fact]
    public void UserProfile_SetMainEmail_WhenEmailIsAlreadyMain_DoesNotEmitDomainEvent()
    {
        var profile = CreateExistingProfile();
        var email = Assert.Single(profile.Emails);
        profile.ClearEvents();

        profile.SetMainEmail(email.Id);

        Assert.Empty(profile.DomainEvents);
    }

    [Fact]
    public void UserProfile_ConfirmEmail_MarksEmailAsConfirmed()
    {
        var profile = CreateExistingProfile();
        var email = Assert.Single(profile.Emails);
        profile.ClearEvents();

        profile.ConfirmEmail(email.Id);

        Assert.True(email.IsConfirmed);
        var emailConfirmedEvent = Assert.Single(profile.DomainEvents.OfType<EmailConfirmedDomainEvent>());
        Assert.Equal(profile.Id, emailConfirmedEvent.UserProfileId);
        Assert.Equal(email.Id, emailConfirmedEvent.EmailId);
        Assert.Equal(email.Address, emailConfirmedEvent.Email);
        var stateChangedEvent = Assert.Single(profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
        Assert.Equal(email.Address.Value, stateChangedEvent.AggregateState.MainEmail);
        Assert.True(stateChangedEvent.AggregateState.IsMainEmailConfirmed);
    }

    [Fact]
    public void UserProfile_ConfirmEmail_WhenEmailDoesNotExist_Throws()
    {
        var profile = CreateExistingProfile();

        Assert.Throws<InvalidOperationException>(() => profile.ConfirmEmail(Id<Email>.New()));
    }

    [Fact]
    public void UserProfile_ConfirmEmail_WhenEmailIsAlreadyConfirmed_DoesNotEmitDomainEvent()
    {
        var profile = CreateExistingProfile();
        var email = Assert.Single(profile.Emails);
        profile.ConfirmEmail(email.Id);
        profile.ClearEvents();

        profile.ConfirmEmail(email.Id);

        Assert.Empty(profile.DomainEvents);
    }

    [Fact]
    public void UserProfile_AddPhone_AddsPhoneToAggregate()
    {
        var profile = CreateExistingProfile();
        var mainEmail = Assert.Single(profile.Emails);

        var phone = profile.AddPhone("+48123123123");

        Assert.Single(profile.Phones);
        Assert.Equal(phone.Id, profile.Phones[0].Id);
        Assert.Equal("+48123123123", profile.Phones[0].Number.Value);
        Assert.Equal(profile.Id, profile.Phones[0].UserProfileId);
        Assert.True(profile.Phones[0].IsMain);
        var @event = Assert.IsType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>(
            Assert.Single(profile.DomainEvents));
        Assert.Equal(mainEmail.Address.Value, @event.AggregateState.MainEmail);
        Assert.Equal("+48123123123", @event.AggregateState.MainPhone);
    }

    [Fact]
    public void UserProfile_AddPhone_WithDuplicateNumber_Throws()
    {
        var profile = CreateExistingProfile();
        profile.AddPhone("+48123123123");

        Assert.Throws<InvalidOperationException>(() => profile.AddPhone("+48123123123"));
    }

    [Fact]
    public void UserProfile_AddPhone_WithSameNumberInDifferentFormat_Throws()
    {
        var profile = CreateExistingProfile();
        profile.AddPhone("+48 123 123 123");

        Assert.Throws<InvalidOperationException>(() => profile.AddPhone("+48123123123"));
    }

    [Fact]
    public void UserProfile_AddPhone_SecondPhone_IsNotMain()
    {
        var profile = CreateExistingProfile();
        profile.AddPhone("+48123123123");

        var secondPhone = profile.AddPhone("+48987654321");

        Assert.False(secondPhone.IsMain);
    }

    [Fact]
    public void UserProfile_SetMainPhone_SwitchesMainFlag()
    {
        var profile = CreateExistingProfile();
        var firstPhone = profile.AddPhone("+48123123123");
        var secondPhone = profile.AddPhone("+48987654321");
        profile.ClearEvents();

        profile.SetMainPhone(secondPhone.Id);

        Assert.False(firstPhone.IsMain);
        Assert.True(secondPhone.IsMain);
        Assert.Single(profile.Phones, x => x.IsMain);
        var phoneChangedEvent = Assert.Single(profile.DomainEvents.OfType<MainPhoneChangedDomainEvent>());
        Assert.Equal(profile.Id.Value, phoneChangedEvent.AggregateId);
        Assert.Equal(profile.Id, phoneChangedEvent.UserProfileId);
        Assert.Equal(secondPhone.Id, phoneChangedEvent.PhoneId);
        Assert.Equal(secondPhone.Number, phoneChangedEvent.Number);
        var stateChangedEvent = Assert.Single(profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
        Assert.Equal(secondPhone.Number.Value, stateChangedEvent.AggregateState.MainPhone);
    }

    [Fact]
    public void UserProfile_SetMainPhone_WhenPhoneDoesNotExist_Throws()
    {
        var profile = CreateExistingProfile();
        profile.AddPhone("+48123123123");

        Assert.Throws<InvalidOperationException>(() => profile.SetMainPhone(Id<Phone>.New()));
    }

    [Fact]
    public void UserProfile_SetMainPhone_WhenPhoneIsAlreadyMain_DoesNotEmitDomainEvent()
    {
        var profile = CreateExistingProfile();
        var phone = profile.AddPhone("+48123123123");
        profile.ClearEvents();

        profile.SetMainPhone(phone.Id);

        Assert.Empty(profile.DomainEvents);
    }

    [Fact]
    public void Phone_Create_WithInvalidNumber_Throws()
    {
        var userProfileId = Id<UserProfile>.New();

        var exception = Assert.Throws<ArgumentException>(() => Phone.Create(userProfileId, "123123123"));

        Assert.StartsWith(PhoneNumber.InvalidPhoneNumberMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Phone_Create_NormalizesNumberToE164()
    {
        var userProfileId = Id<UserProfile>.New();

        var phone = Phone.Create(userProfileId, "+48 123 123 123");

        Assert.Equal("+48123123123", phone.Number.Value);
    }

    [Fact]
    public void UserProfile_Create_WithoutEmail_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => UserProfile.Create("jdoe", "John Doe", null!, id: Id<UserProfile>.New()));
    }

    [Fact]
    public void UserProfile_Create_WithEmailAndPhone_Succeeds()
    {
        var id = Id<UserProfile>.New();
        var profile = UserProfile.Create(
            "jdoe",
            "John Doe",
            EmailAddress.Create("john@example.com"),
            PhoneNumber.Create("+48123123123"),
            id: id);

        Assert.Single(profile.Phones);
        Assert.True(profile.Phones[0].IsMain);
        Assert.Single(profile.Emails);
        Assert.True(profile.Emails[0].IsMain);
        Assert.True(profile.Emails[0].IsAuth);
    }

    [Fact]
    public void UserProfile_Create_WithEmailAndPhone_EmitsUserProfileCreatedDomainEvent()
    {
        var id = Id<UserProfile>.New();
        var profile = UserProfile.Create(
            " jdoe ",
            " John Doe ",
            EmailAddress.Create("john@example.com"),
            PhoneNumber.Create("+48123123123"),
            " https://cdn.example/avatar.png ",
            " about me ",
            isActive: false,
            lastSeenAtUtc: new DateTime(2026, 3, 10, 8, 30, 0, DateTimeKind.Utc),
            isEmailVisible: false,
            isPhoneVisible: true,
            id: id);

        var createdEvent = Assert.Single(profile.DomainEvents.OfType<UserProfileCreatedDomainEvent>());
        Assert.Equal(id.Value, createdEvent.AggregateId);
        Assert.Equal(id, createdEvent.UserProfileId);
        Assert.Equal(profile.Emails.Single().Id, createdEvent.MainEmailId);
        Assert.Equal("jdoe", createdEvent.UserName);
        Assert.Equal("John Doe", createdEvent.DisplayName);
        Assert.Equal(EmailAddress.Create("john@example.com"), createdEvent.MainEmail);
        Assert.Equal(PhoneNumber.Create("+48123123123"), createdEvent.MainPhone);
        Assert.Equal("https://cdn.example/avatar.png", createdEvent.AvatarUrl);
        Assert.Equal("about me", createdEvent.Bio);
        Assert.False(createdEvent.IsActive);
        Assert.Equal(new DateTime(2026, 3, 10, 8, 30, 0, DateTimeKind.Utc), createdEvent.LastSeenAtUtc);
        Assert.False(createdEvent.IsEmailVisible);
        Assert.True(createdEvent.IsPhoneVisible);
        var stateChangedEvent = Assert.Single(profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
        Assert.Equal("john@example.com", stateChangedEvent.AggregateState.MainEmail);
        Assert.False(stateChangedEvent.AggregateState.IsMainEmailConfirmed);
        Assert.Equal("+48123123123", stateChangedEvent.AggregateState.MainPhone);
        Assert.Empty(profile.DomainEvents.OfType<EmailAddedDomainEvent>());
    }

    [Fact]
    public void UserProfile_Create_WithEmailOnly_EmitsUserProfileCreatedDomainEvent()
    {
        var id = Id<UserProfile>.New();
        var profile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create("john@example.com"), id: id);

        var createdEvent = Assert.Single(profile.DomainEvents.OfType<UserProfileCreatedDomainEvent>());
        Assert.Equal(profile.Emails.Single().Id, createdEvent.MainEmailId);
        Assert.Equal(EmailAddress.Create("john@example.com"), createdEvent.MainEmail);
        Assert.Null(createdEvent.MainPhone);
        var stateChangedEvent = Assert.Single(profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>());
        Assert.Equal("john@example.com", stateChangedEvent.AggregateState.MainEmail);
        Assert.False(stateChangedEvent.AggregateState.IsMainEmailConfirmed);
        Assert.Null(stateChangedEvent.AggregateState.MainPhone);
        Assert.Empty(profile.DomainEvents.OfType<EmailAddedDomainEvent>());
    }

    [Fact]
    public void UserProfile_Create_WithEmailOnly_DoesNotCreatePhone()
    {
        var id = Id<UserProfile>.New();
        var profile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create("john@example.com"), id: id);

        Assert.Empty(profile.Phones);
    }

    private static UserProfile CreateExistingProfile()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create("john@example.com"), id: Id<UserProfile>.New());
        profile.ClearEvents();
        return profile;
    }
}
