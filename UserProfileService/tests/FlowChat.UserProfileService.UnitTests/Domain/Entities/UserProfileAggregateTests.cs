using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserProfileAggregateTests
{
    [Fact]
    public void UserProfile_Create_WithTypedId_AssignsTypedAggregateId()
    {
        var id = Id<UserProfile>.New();

        var profile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create("john@example.com"), id: id);

        profile.Id.Should().Be(id);
        profile.Id.Value.Should().Be(id.Value);
        profile.DomainEvents.OfType<UserProfileCreatedDomainEvent>().Should().ContainSingle();
        profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>().Should().ContainSingle();
    }

    [Fact]
    public void Email_Create_WithInvalidAddress_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => EmailAddress.Create("not-an-email"));

        exception.Message.Should().StartWith(EmailAddress.InvalidEmailAddressMessage);
    }

    [Fact]
    public void Email_Create_NormalizesTrimmedAddress()
    {
        var userProfileId = Id<UserProfile>.New();

        var email = Email.Create(userProfileId, EmailAddress.Create(" john@example.com "));

        email.Address.Value.Should().Be("john@example.com");
        email.IsConfirmed.Should().BeFalse();
        email.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void UserProfile_AddEmail_AddsSecondaryEmailToAggregate()
    {
        var profile = CreateExistingProfile();
        var existingMainEmail = profile.Emails.Should().ContainSingle().Subject;

        var email = profile.AddEmail(EmailAddress.Create("john.secondary@example.com"));

        profile.Emails.Should().HaveCount(2);
        profile.Emails.Single(x => x.Address.Value == "john.secondary@example.com").Id.Should().Be(email.Id);
        email.Address.Value.Should().Be("john.secondary@example.com");
        email.UserProfileId.Should().Be(profile.Id);
        email.IsMain.Should().BeFalse();
        email.IsAuth.Should().BeFalse();
        email.IsConfirmed.Should().BeFalse();
        email.IsVisible.Should().BeTrue();
        existingMainEmail.IsMain.Should().BeTrue();

        var emailAddedEvent = profile.DomainEvents.OfType<EmailAddedDomainEvent>().Should().ContainSingle().Subject;
        emailAddedEvent.UserProfileId.Should().Be(profile.Id);
        emailAddedEvent.EmailId.Should().Be(email.Id);
        emailAddedEvent.Email.Should().Be(email.Address);

        var @event = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>()
            .Should().ContainSingle().Subject;
        @event.AggregateState.MainEmail.Should().Be(existingMainEmail.Address.Value);
        @event.AggregateState.IsMainEmailConfirmed.Should().BeFalse();
        @event.AggregateState.MainPhone.Should().BeNull();
    }

    [Fact]
    public void UserProfile_AddEmail_WithDuplicateAddress_Throws()
    {
        var profile = CreateExistingProfile();

        Assert.Throws<InvalidOperationException>(() => profile.AddEmail(EmailAddress.Create("JOHN@example.com")));
    }

    [Fact]
    public void UserProfile_SetMainEmail_SwitchesMainFlag()
    {
        var profile = CreateExistingProfile();
        var firstEmail = profile.Emails.Should().ContainSingle().Subject;
        var secondEmail = profile.AddEmail(EmailAddress.Create("john.secondary@example.com"));
        profile.ClearEvents();

        profile.SetMainEmail(secondEmail.Id);

        firstEmail.IsMain.Should().BeFalse();
        secondEmail.IsMain.Should().BeTrue();
        profile.Emails.Should().ContainSingle(x => x.IsMain);

        var emailChangedEvent = profile.DomainEvents.OfType<MainEmailChangedDomainEvent>().Should().ContainSingle().Subject;
        emailChangedEvent.AggregateId.Should().Be(profile.Id.Value);
        emailChangedEvent.UserProfileId.Should().Be(profile.Id);
        emailChangedEvent.EmailId.Should().Be(secondEmail.Id);
        emailChangedEvent.Address.Should().Be(secondEmail.Address);

        var stateChangedEvent = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.MainEmail.Should().Be(secondEmail.Address.Value);
        stateChangedEvent.AggregateState.IsMainEmailConfirmed.Should().BeFalse();
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
        var email = profile.Emails.Should().ContainSingle().Subject;
        profile.ClearEvents();

        profile.SetMainEmail(email.Id);

        profile.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UserProfile_SetAuthEmail_SwitchesAuthFlag()
    {
        var profile = CreateExistingProfile();
        var firstEmail = profile.Emails.Should().ContainSingle().Subject;
        var secondEmail = profile.AddEmail(EmailAddress.Create("john.secondary@example.com"));
        profile.ClearEvents();

        profile.SetAuthEmail(secondEmail.Id);

        firstEmail.IsAuth.Should().BeFalse();
        secondEmail.IsAuth.Should().BeTrue();
        profile.Emails.Should().ContainSingle(x => x.IsAuth);

        var stateChangedEvent = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.MainEmail.Should().Be(firstEmail.Address.Value);
        stateChangedEvent.AggregateState.IsMainEmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public void UserProfile_SetAuthEmail_WhenEmailDoesNotExist_Throws()
    {
        var profile = CreateExistingProfile();

        Assert.Throws<InvalidOperationException>(() => profile.SetAuthEmail(Id<Email>.New()));
    }

    [Fact]
    public void UserProfile_SetAuthEmail_WhenEmailIsAlreadyAuth_DoesNotEmitDomainEvent()
    {
        var profile = CreateExistingProfile();
        var email = profile.Emails.Should().ContainSingle().Subject;
        profile.ClearEvents();

        profile.SetAuthEmail(email.Id);

        profile.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UserProfile_ConfirmEmail_MarksEmailAsConfirmed()
    {
        var profile = CreateExistingProfile();
        var email = profile.Emails.Should().ContainSingle().Subject;
        profile.ClearEvents();

        profile.ConfirmEmail(email.Id);

        email.IsConfirmed.Should().BeTrue();

        var emailConfirmedEvent = profile.DomainEvents.OfType<EmailConfirmedDomainEvent>().Should().ContainSingle().Subject;
        emailConfirmedEvent.UserProfileId.Should().Be(profile.Id);
        emailConfirmedEvent.EmailId.Should().Be(email.Id);
        emailConfirmedEvent.Email.Should().Be(email.Address);
        emailConfirmedEvent.IsAuth.Should().BeTrue();

        var stateChangedEvent = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.MainEmail.Should().Be(email.Address.Value);
        stateChangedEvent.AggregateState.IsMainEmailConfirmed.Should().BeTrue();
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
        var email = profile.Emails.Should().ContainSingle().Subject;
        profile.ConfirmEmail(email.Id);
        profile.ClearEvents();

        profile.ConfirmEmail(email.Id);

        profile.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UserProfile_AddPhone_AddsPhoneToAggregate()
    {
        var profile = CreateExistingProfile();
        var mainEmail = profile.Emails.Should().ContainSingle().Subject;

        var phone = profile.AddPhone(PhoneNumber.Create("+48123123123"));

        profile.Phones.Should().ContainSingle();
        profile.Phones[0].Id.Should().Be(phone.Id);
        profile.Phones[0].Number.Value.Should().Be("+48123123123");
        profile.Phones[0].UserProfileId.Should().Be(profile.Id);
        profile.Phones[0].IsMain.Should().BeTrue();
        profile.Phones[0].IsVisible.Should().BeTrue();

        var @event = profile.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>().Subject;
        @event.AggregateState.MainEmail.Should().Be(mainEmail.Address.Value);
        @event.AggregateState.MainPhone.Should().Be("+48123123123");
    }

    [Fact]
    public void UserProfile_AddPhone_WithDuplicateNumber_Throws()
    {
        var profile = CreateExistingProfile();
        profile.AddPhone(PhoneNumber.Create("+48123123123"));

        Assert.Throws<InvalidOperationException>(() => profile.AddPhone(PhoneNumber.Create("+48123123123")));
    }

    [Fact]
    public void UserProfile_AddPhone_WithSameNumberInDifferentFormat_Throws()
    {
        var profile = CreateExistingProfile();
        profile.AddPhone(PhoneNumber.Create("+48 123 123 123"));

        Assert.Throws<InvalidOperationException>(() => profile.AddPhone(PhoneNumber.Create("+48123123123")));
    }

    [Fact]
    public void UserProfile_AddPhone_SecondPhone_IsNotMain()
    {
        var profile = CreateExistingProfile();
        profile.AddPhone(PhoneNumber.Create("+48123123123"));

        var secondPhone = profile.AddPhone(PhoneNumber.Create("+48987654321"));

        secondPhone.IsMain.Should().BeFalse();
    }

    [Fact]
    public void UserProfile_SetMainPhone_SwitchesMainFlag()
    {
        var profile = CreateExistingProfile();
        var firstPhone = profile.AddPhone(PhoneNumber.Create("+48123123123"));
        var secondPhone = profile.AddPhone(PhoneNumber.Create("+48987654321"));
        profile.ClearEvents();

        profile.SetMainPhone(secondPhone.Id);

        firstPhone.IsMain.Should().BeFalse();
        secondPhone.IsMain.Should().BeTrue();
        profile.Phones.Should().ContainSingle(x => x.IsMain);

        var phoneChangedEvent = profile.DomainEvents.OfType<MainPhoneChangedDomainEvent>().Should().ContainSingle().Subject;
        phoneChangedEvent.AggregateId.Should().Be(profile.Id.Value);
        phoneChangedEvent.UserProfileId.Should().Be(profile.Id);
        phoneChangedEvent.PhoneId.Should().Be(secondPhone.Id);
        phoneChangedEvent.Number.Should().Be(secondPhone.Number);

        var stateChangedEvent = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.MainPhone.Should().Be(secondPhone.Number.Value);
    }

    [Fact]
    public void UserProfile_SetMainPhone_WhenPhoneDoesNotExist_Throws()
    {
        var profile = CreateExistingProfile();
        profile.AddPhone(PhoneNumber.Create("+48123123123"));

        Assert.Throws<InvalidOperationException>(() => profile.SetMainPhone(Id<Phone>.New()));
    }

    [Fact]
    public void UserProfile_SetMainPhone_WhenPhoneIsAlreadyMain_DoesNotEmitDomainEvent()
    {
        var profile = CreateExistingProfile();
        var phone = profile.AddPhone(PhoneNumber.Create("+48123123123"));
        profile.ClearEvents();

        profile.SetMainPhone(phone.Id);

        profile.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Phone_Create_WithInvalidNumber_Throws()
    {
        var userProfileId = Id<UserProfile>.New();

        var exception = Assert.Throws<ArgumentException>(() => PhoneNumber.Create("123123123"));

        exception.Message.Should().StartWith(PhoneNumber.InvalidPhoneNumberMessage);
    }

    [Fact]
    public void Phone_Create_NormalizesNumberToE164()
    {
        var userProfileId = Id<UserProfile>.New();

        var phone = Phone.Create(userProfileId, PhoneNumber.Create("+48 123 123 123"));

        phone.Number.Value.Should().Be("+48123123123");
        phone.IsVisible.Should().BeTrue();
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

        profile.Phones.Should().ContainSingle();
        profile.Phones[0].IsMain.Should().BeTrue();
        profile.Phones[0].IsVisible.Should().BeTrue();
        profile.Emails.Should().ContainSingle();
        profile.Emails[0].IsMain.Should().BeTrue();
        profile.Emails[0].IsAuth.Should().BeTrue();
        profile.Emails[0].IsVisible.Should().BeTrue();
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
            lastSeenAtUtc: new DateTimeOffset(2026, 3, 10, 8, 30, 0, TimeSpan.Zero),
            id: id);

        var createdEvent = profile.DomainEvents.OfType<UserProfileCreatedDomainEvent>().Should().ContainSingle().Subject;
        createdEvent.AggregateId.Should().Be(id.Value);
        createdEvent.UserProfileId.Should().Be(id);
        createdEvent.MainEmailId.Should().Be(profile.Emails.Single().Id);
        createdEvent.FriendlyUserId.Should().Be("jdoe");
        createdEvent.DisplayName.Should().Be("John Doe");
        createdEvent.MainEmail.Should().Be(EmailAddress.Create("john@example.com"));
        createdEvent.MainPhone.Should().Be(PhoneNumber.Create("+48123123123"));
        createdEvent.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        createdEvent.Bio.Should().Be("about me");
        createdEvent.IsActive.Should().BeFalse();
        createdEvent.LastSeenAtUtc.Should().Be(new DateTimeOffset(2026, 3, 10, 8, 30, 0, TimeSpan.Zero));

        var stateChangedEvent = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.MainEmail.Should().Be("john@example.com");
        stateChangedEvent.AggregateState.IsMainEmailConfirmed.Should().BeFalse();
        stateChangedEvent.AggregateState.MainPhone.Should().Be("+48123123123");
        profile.DomainEvents.OfType<EmailAddedDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public void UserProfile_Create_WithEmailOnly_EmitsUserProfileCreatedDomainEvent()
    {
        var id = Id<UserProfile>.New();
        var profile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create("john@example.com"), id: id);

        var createdEvent = profile.DomainEvents.OfType<UserProfileCreatedDomainEvent>().Should().ContainSingle().Subject;
        createdEvent.MainEmailId.Should().Be(profile.Emails.Single().Id);
        createdEvent.MainEmail.Should().Be(EmailAddress.Create("john@example.com"));
        createdEvent.MainPhone.Should().BeNull();

        var stateChangedEvent = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.MainEmail.Should().Be("john@example.com");
        stateChangedEvent.AggregateState.IsMainEmailConfirmed.Should().BeFalse();
        stateChangedEvent.AggregateState.MainPhone.Should().BeNull();
        profile.DomainEvents.OfType<EmailAddedDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public void UserProfile_Create_WithPersonalFields_StoresThemInAggregateAndSnapshot()
    {
        var profile = UserProfile.Create(
            "jdoe",
            "John Doe",
            EmailAddress.Create("john@example.com"),
            id: Id<UserProfile>.New(),
            firstName: " John ",
            lastName: " Doe ",
            organization: " FlowChat ");

        profile.FirstName.Should().Be("John");
        profile.LastName.Should().Be("Doe");
        profile.Organization.Should().Be("FlowChat");

        var createdEvent = profile.DomainEvents.OfType<UserProfileCreatedDomainEvent>().Should().ContainSingle().Subject;
        createdEvent.FirstName.Should().Be("John");
        createdEvent.LastName.Should().Be("Doe");
        createdEvent.Organization.Should().Be("FlowChat");

        var stateChangedEvent = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileSnapshot>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.FirstName.Should().Be("John");
        stateChangedEvent.AggregateState.LastName.Should().Be("Doe");
        stateChangedEvent.AggregateState.Organization.Should().Be("FlowChat");
    }

    [Fact]
    public void UserProfile_Create_WithEmailOnly_DoesNotCreatePhone()
    {
        var id = Id<UserProfile>.New();
        var profile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create("john@example.com"), id: id);

        profile.Phones.Should().BeEmpty();
    }

    private static UserProfile CreateExistingProfile()
    {
        var profile = UserProfile.Create("jdoe", "John Doe", EmailAddress.Create("john@example.com"), id: Id<UserProfile>.New());
        profile.ClearEvents();
        return profile;
    }
}
