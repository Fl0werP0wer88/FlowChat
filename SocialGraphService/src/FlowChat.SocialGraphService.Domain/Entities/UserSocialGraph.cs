using FlowChat.SocialGraphService.Domain.Common;
using FlowChat.SocialGraphService.Domain.Common.Contracts;

namespace FlowChat.SocialGraphService.Domain.Entities
{
    public class UserSocialGraph : AggregateRootBase<UserSocialGraph>
    {
        public IReadOnlyList<Contact> Contacts { get; } = new List<Contact>();
        public IReadOnlyList<Invitation> Invitations { get; } = new List<Invitation>();
        public Id<UserSocialGraph> UserId { get; } = Id<UserSocialGraph>.New();
        public UserSocialGraph(Id<UserSocialGraph> id) : base(id)
        {

        }

        public UserSocialGraph(Guid id) : this(Id<UserSocialGraph>.FromGuid(id))
        {

        }
    }
}
