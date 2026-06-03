using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.UnitTests;

public sealed class AccountTests
{
    [Fact]
    public void Create_WithValidData_InitializesStateAndEmitsRegistrationEvent()
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

        account.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AccountRegisteredDomainEvent>().Subject.Should().Match<AccountRegisteredDomainEvent>(x =>
                x.FriendlyUserId == "flower"
                && x.FirstName == "Flower"
                && x.LastName == "Power"
                && x.Organization == "FlowChat");
    }

    [Fact]
    public void ConfirmEmail_WhenEmailNotConfirmed_MarksConfirmedAndEmitsAccountConfirmedEvent()
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
    }

    [Fact]
    public void ChangeAuthEmail_WhenEmailChanges_UpdatesEmailAndSecurityStamp()
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
        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordFailedLogin_IncrementsFailedCount()
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
        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ResetFailedLogins_WhenCountIsPositive_ResetsFailedCount()
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
        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RotateSecurityStamp_ChangesStamp()
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
        account.DomainEvents.Should().BeEmpty();
    }
}
