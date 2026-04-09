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

        var profile = UserProfile.Create("jdoe", EmailAddress.Create("john@example.com"), id: id);

        profile.Id.Should().Be(id);
        profile.Id.Value.Should().Be(id.Value);
        profile.DomainEvents.OfType<UserProfileCreatedDomainEvent>().Should().ContainSingle();
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
    public void Email_SetMain_WhenEmailIsNotConfirmed_Throws()
    {
        var email = Email.Create(Id<UserProfile>.New(), EmailAddress.Create("john@example.com"));

        var action = () => email.SetMain(true);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Email 'john@example.com' must be confirmed before it can be set as main.");
    }

    [Fact]
    public void Email_SetMain_WhenClearingFlagOnUnconfirmedEmail_Succeeds()
    {
        var email = Email.Create(Id<UserProfile>.New(), EmailAddress.Create("john@example.com"), isMain: true);

        email.SetMain(false);

        email.IsMain.Should().BeFalse();
    }

    [Fact]
    public void Email_SetAuth_WhenEmailIsNotConfirmed_Throws()
    {
        var email = Email.Create(Id<UserProfile>.New(), EmailAddress.Create("john@example.com"));

        var action = () => email.SetAuth(true);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Email 'john@example.com' must be confirmed before it can be set as auth.");
    }

    [Fact]
    public void Email_SetAuth_WhenClearingFlagOnUnconfirmedEmail_Succeeds()
    {
        var email = Email.Create(Id<UserProfile>.New(), EmailAddress.Create("john@example.com"), isAuth: true);

        email.SetAuth(false);

        email.IsAuth.Should().BeFalse();
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

        var @event = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileState>>()
            .Should().ContainSingle().Subject;
        @event.AggregateState.Id.Should().Be(profile.Id.Value);
        @event.AggregateState.NormalizedFriendlyUserId.Should().Be(profile.NormalizedFriendlyUserId);
        @event.AggregateState.Emails.Should().ContainSingle(x =>
            x.Id == existingMainEmail.Id.Value &&
            x.Address == existingMainEmail.Address.Value &&
            x.IsMain &&
            !x.IsConfirmed);
        @event.AggregateState.Phones.Should().BeEmpty();
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
        profile.ConfirmEmail(secondEmail.Id);
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

        var stateChangedEvent = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileState>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.Emails.Should().ContainSingle(x =>
            x.Id == secondEmail.Id.Value &&
            x.Address == secondEmail.Address.Value &&
            x.IsMain &&
            x.IsConfirmed);
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
    public void UserProfile_SetMainEmail_WhenEmailIsNotConfirmed_Throws()
    {
        var profile = CreateExistingProfile();
        var secondEmail = profile.AddEmail(EmailAddress.Create("john.secondary@example.com"));

        var action = () => profile.SetMainEmail(secondEmail.Id);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Email '{secondEmail.Address.Value}' must be confirmed before it can be set as main.");
        profile.Emails.Should().ContainSingle(x => x.IsMain && x.Id != secondEmail.Id);
    }

    [Fact]
    public void UserProfile_SetAuthEmail_SwitchesAuthFlag()
    {
        var profile = CreateExistingProfile();
        var firstEmail = profile.Emails.Should().ContainSingle().Subject;
        var secondEmail = profile.AddEmail(EmailAddress.Create("john.secondary@example.com"));
        profile.ConfirmEmail(secondEmail.Id);
        profile.ClearEvents();

        profile.SetAuthEmail(secondEmail.Id);

        firstEmail.IsAuth.Should().BeFalse();
        secondEmail.IsAuth.Should().BeTrue();
        profile.Emails.Should().ContainSingle(x => x.IsAuth);

        var stateChangedEvent = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileState>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.Emails.Should().ContainSingle(x =>
            x.Id == firstEmail.Id.Value &&
            x.Address == firstEmail.Address.Value &&
            x.IsMain &&
            !x.IsConfirmed);
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
    public void UserProfile_SetAuthEmail_WhenEmailIsNotConfirmed_Throws()
    {
        var profile = CreateExistingProfile();
        var secondEmail = profile.AddEmail(EmailAddress.Create("john.secondary@example.com"));

        var action = () => profile.SetAuthEmail(secondEmail.Id);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Email '{secondEmail.Address.Value}' must be confirmed before it can be set as auth.");
        profile.Emails.Should().ContainSingle(x => x.IsAuth && x.Id != secondEmail.Id);
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

        var stateChangedEvent = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileState>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.Emails.Should().ContainSingle(x =>
            x.Id == email.Id.Value &&
            x.Address == email.Address.Value &&
            x.IsMain &&
            x.IsConfirmed);
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
        profile.Phones[0].IsConfirmed.Should().BeFalse();
        profile.Phones[0].IsVisible.Should().BeTrue();

        var @event = profile.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileState>>().Subject;
        @event.AggregateState.Emails.Should().ContainSingle(x =>
            x.Id == mainEmail.Id.Value &&
            x.Address == mainEmail.Address.Value &&
            x.IsMain);
        @event.AggregateState.Phones.Should().ContainSingle(x => x.Number == "+48123123123" && x.IsMain);
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
        secondPhone.Confirm();
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

        var stateChangedEvent = profile.DomainEvents.OfType<AggregateStateChangedDomainEvent<UserProfile, UserProfileState>>()
            .Should().ContainSingle().Subject;
        stateChangedEvent.AggregateState.Phones.Should().ContainSingle(x =>
            x.Id == secondPhone.Id.Value &&
            x.Number == secondPhone.Number.Value &&
            x.IsMain);
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
    public void UserProfile_SetMainPhone_WhenPhoneIsNotConfirmed_Throws()
    {
        var profile = CreateExistingProfile();
        profile.AddPhone(PhoneNumber.Create("+48123123123"));
        var secondPhone = profile.AddPhone(PhoneNumber.Create("+48987654321"));

        var action = () => profile.SetMainPhone(secondPhone.Id);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Phone '{secondPhone.Number.Value}' must be confirmed before it can be set as main.");
        profile.Phones.Should().ContainSingle(x => x.IsMain && x.Id != secondPhone.Id);
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
        phone.IsConfirmed.Should().BeFalse();
        phone.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void Phone_Rehydrate_WithConfirmedPhone_RestoresConfirmedState()
    {
        var phone = Phone.Rehydrate(
            Id<UserProfile>.New(),
            PhoneNumber.Create("+48123123123"),
            isConfirmed: true);

        phone.IsConfirmed.Should().BeTrue();
    }

    [Fact]
    public void Phone_SetMain_WhenPhoneIsNotConfirmed_Throws()
    {
        var phone = Phone.Create(Id<UserProfile>.New(), PhoneNumber.Create("+48123123123"));

        var action = () => phone.SetMain(true);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Phone '+48123123123' must be confirmed before it can be set as main.");
    }

    [Fact]
    public void Phone_SetMain_WhenClearingFlagOnUnconfirmedPhone_Succeeds()
    {
        var phone = Phone.Create(Id<UserProfile>.New(), PhoneNumber.Create("+48123123123"), isMain: true);

        phone.SetMain(false);

        phone.IsMain.Should().BeFalse();
    }

    [Fact]
    public void UserProfile_Create_WithoutEmail_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => UserProfile.Create("jdoe", null!, id: Id<UserProfile>.New()));
    }

    [Fact]
    public void UserProfile_Create_WithEmailAndPhone_Succeeds()
    {
        var id = Id<UserProfile>.New();
        var profile = UserProfile.Create(
            "jdoe",
            EmailAddress.Create("john@example.com"),
            PhoneNumber.Create("+48123123123"),
            id: id);

        profile.Phones.Should().ContainSingle();
        profile.Phones[0].IsMain.Should().BeTrue();
        profile.Phones[0].IsConfirmed.Should().BeFalse();
        profile.Phones[0].IsVisible.Should().BeTrue();
        profile.Emails.Should().ContainSingle();
        profile.Emails[0].IsMain.Should().BeTrue();
        profile.Emails[0].IsAuth.Should().BeTrue();
        profile.Emails[0].IsConfirmed.Should().BeFalse();
        profile.Emails[0].IsVisible.Should().BeTrue();
    }

    [Fact]
    public void UserProfile_Create_WithEmailAndPhone_EmitsUserProfileCreatedDomainEvent()
    {
        var id = Id<UserProfile>.New();
        var profile = UserProfile.Create(
            " jdoe ",
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
        createdEvent.MainEmail.Should().Be(EmailAddress.Create("john@example.com"));
        createdEvent.MainPhone.Should().Be(PhoneNumber.Create("+48123123123"));
        createdEvent.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        createdEvent.Bio.Should().Be("about me");
        createdEvent.IsActive.Should().BeFalse();
        createdEvent.LastSeenAtUtc.Should().Be(new DateTimeOffset(2026, 3, 10, 8, 30, 0, TimeSpan.Zero));

        profile.DomainEvents.OfType<EmailAddedDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public void UserProfile_Create_WithEmailOnly_EmitsUserProfileCreatedDomainEvent()
    {
        var id = Id<UserProfile>.New();
        var profile = UserProfile.Create("jdoe", EmailAddress.Create("john@example.com"), id: id);

        var createdEvent = profile.DomainEvents.OfType<UserProfileCreatedDomainEvent>().Should().ContainSingle().Subject;
        createdEvent.MainEmailId.Should().Be(profile.Emails.Single().Id);
        createdEvent.MainEmail.Should().Be(EmailAddress.Create("john@example.com"));
        createdEvent.MainPhone.Should().BeNull();

        profile.DomainEvents.OfType<EmailAddedDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public void UserProfile_Create_WithPersonalFields_StoresThemInAggregateAndSnapshot()
    {
        var profile = UserProfile.Create(
            "jdoe",
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

    }

    [Fact]
    public void UserProfile_Create_WithEmailOnly_DoesNotCreatePhone()
    {
        var id = Id<UserProfile>.New();
        var profile = UserProfile.Create("jdoe", EmailAddress.Create("john@example.com"), id: id);

        profile.Phones.Should().BeEmpty();
    }

    private static UserProfile CreateExistingProfile()
    {
        var profile = UserProfile.Create("jdoe", EmailAddress.Create("john@example.com"), id: Id<UserProfile>.New());
        profile.ClearEvents();
        return profile;
    }
}
