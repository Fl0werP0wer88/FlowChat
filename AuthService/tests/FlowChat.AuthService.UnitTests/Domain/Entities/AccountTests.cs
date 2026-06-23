using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.UnitTests;

public sealed class AccountTests
{
    [Fact]
    public void Create_WithValidData_InitializesStateAndEmitsRegistrationAndSnapshotEvents()
    {
        var account = Account.Create(
            Id<Account>.New(),
            "  flower  ",
            EmailAddress.Create(" Flower@example.com "),
            "hashed-password",
            "security-stamp",
            " Flower ",
            " Power ",
            " FlowChat ");

        account.FriendlyUserId.Value.Should().Be("flower");
        account.Email.Should().Be(EmailAddress.Create("flower@example.com"));
        account.PasswordHash.Should().Be("hashed-password");
        account.SecurityStamp.Should().Be("security-stamp");
        account.AccessFailedCount.Should().Be(0);
        account.IsEmailConfirmed.Should().BeFalse();

        account.DomainEvents.OfType<AccountRegisteredDomainEvent>().Should().ContainSingle()
            .Which.Should().Match<AccountRegisteredDomainEvent>(x =>
                x.FriendlyUserId == "flower"
                && x.FirstName == "Flower"
                && x.LastName == "Power"
                && x.Organization == "FlowChat");

        account.DomainEvents.OfType<AggregateStateChangedDomainEvent<Account, AccountSnapshot>>().Should().ContainSingle()
            .Which.AggregateState.Should().Be(new AccountSnapshot(
                account.Id.Value,
                "flower",
                "flower@example.com",
                "security-stamp",
                0,
                false));
    }

    [Fact]
    public void ConfirmEmail_WhenEmailNotConfirmed_MarksConfirmedAndEmitsAccountConfirmedAndSnapshotEvents()
    {
        var account = Account.Restore(
            Guid.NewGuid(),
            "flower",
            EmailAddress.Create("flower@example.com"),
            "hashed-password",
            "security-stamp",
            0,
            false);

        account.ConfirmEmail();

        account.IsEmailConfirmed.Should().BeTrue();
        account.DomainEvents.OfType<AccountConfirmedDomainEvent>().Should().ContainSingle();
        account.DomainEvents.OfType<AggregateStateChangedDomainEvent<Account, AccountSnapshot>>().Should().ContainSingle()
            .Which.AggregateState.IsEmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public void ChangeAuthEmail_WhenEmailChanges_UpdatesEmailAndSecurityStampAndSnapshot()
    {
        var account = Account.Restore(
            Guid.NewGuid(),
            "flower",
            EmailAddress.Create("flower@example.com"),
            "hashed-password",
            "old-stamp",
            0,
            false);

        account.ChangeAuthEmail(EmailAddress.Create("new@example.com"), "new-stamp");

        account.Email.Should().Be(EmailAddress.Create("new@example.com"));
        account.SecurityStamp.Should().Be("new-stamp");
        account.IsEmailConfirmed.Should().BeTrue();
        account.DomainEvents.OfType<AggregateStateChangedDomainEvent<Account, AccountSnapshot>>().Should().ContainSingle()
            .Which.AggregateState.Should().Be(new AccountSnapshot(
                account.Id.Value,
                "flower",
                "new@example.com",
                "new-stamp",
                0,
                true));
    }

    [Fact]
    public void RecordFailedLogin_IncrementsFailedCountAndUpdatesSnapshot()
    {
        var account = Account.Restore(
            Guid.NewGuid(),
            "flower",
            EmailAddress.Create("flower@example.com"),
            "hashed-password",
            "security-stamp",
            0,
            true);

        account.RecordFailedLogin();

        account.AccessFailedCount.Should().Be(1);
        account.DomainEvents.OfType<AggregateStateChangedDomainEvent<Account, AccountSnapshot>>().Should().ContainSingle()
            .Which.AggregateState.AccessFailedCount.Should().Be(1);
    }

    [Fact]
    public void ResetFailedLogins_WhenCountIsPositive_ResetsFailedCountAndUpdatesSnapshot()
    {
        var account = Account.Restore(
            Guid.NewGuid(),
            "flower",
            EmailAddress.Create("flower@example.com"),
            "hashed-password",
            "security-stamp",
            3,
            true);

        account.ResetFailedLogins();

        account.AccessFailedCount.Should().Be(0);
        account.DomainEvents.OfType<AggregateStateChangedDomainEvent<Account, AccountSnapshot>>().Should().ContainSingle()
            .Which.AggregateState.AccessFailedCount.Should().Be(0);
    }

    [Fact]
    public void RotateSecurityStamp_ChangesStampAndUpdatesSnapshot()
    {
        var account = Account.Restore(
            Guid.NewGuid(),
            "flower",
            EmailAddress.Create("flower@example.com"),
            "hashed-password",
            "old-stamp",
            0,
            true);

        account.RotateSecurityStamp("new-stamp");

        account.SecurityStamp.Should().Be("new-stamp");
        account.DomainEvents.OfType<AggregateStateChangedDomainEvent<Account, AccountSnapshot>>().Should().ContainSingle()
            .Which.AggregateState.SecurityStamp.Should().Be("new-stamp");
    }
}
