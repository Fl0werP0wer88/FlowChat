using FlowChat.Domain.Abstractions;

namespace FlowChat.SocialGraphService.Domain.Entities
{
    public class UserSocialGraph : AggregateRootBase<UserSocialGraph>
    {
        public Guid UserId { get; }
        public string? FirstName { get; }
        public string? LastName { get; }
        public string Login { get; }
        public string? PhoneNumber { get; }
        public string? Email { get; }
        public bool IsPhoneVisible { get; }
        public bool IsEmailVisible { get; }

        private UserSocialGraph(
            Id<UserSocialGraph>? id,
            string login,
            Guid? userId = null,
            string? firstName = null,
            string? lastName = null,
            string? phoneNumber = null,
            string? email = null,
            bool isPhoneVisible = false,
            bool isEmailVisible = false) : base(id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(login);

            UserId = userId ?? Id.Value;
            FirstName = firstName;
            LastName = lastName;
            Login = login;
            PhoneNumber = phoneNumber;
            Email = email;
            IsPhoneVisible = isPhoneVisible;
            IsEmailVisible = isEmailVisible;
        }

        public static UserSocialGraph Create(
            string login,
            Guid? userId = null,
            string? firstName = null,
            string? lastName = null,
            string? phoneNumber = null,
            string? email = null,
            bool isPhoneVisible = false,
            bool isEmailVisible = false,
            Id<UserSocialGraph>? id = null)
        {
            return new UserSocialGraph(
                id,
                login,
                userId,
                firstName,
                lastName,
                phoneNumber,
                email,
                isPhoneVisible,
                isEmailVisible);
        }
    }
}
