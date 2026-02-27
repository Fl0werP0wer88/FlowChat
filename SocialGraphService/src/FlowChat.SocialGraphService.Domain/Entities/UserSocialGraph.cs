using FlowChat.SocialGraphService.Domain.Common;
using FlowChat.SocialGraphService.Domain.Common.Contracts;

namespace FlowChat.SocialGraphService.Domain.Entities
{
    public class UserSocialGraph : AggregateRootBase
    {
        public IReadOnlyList<Contact> Contacts { get; } = new List<Contact>();
        public IReadOnlyList<Invitation> Invitations { get; } = new List<Invitation>();
        public Id<UserSocialGraph> UserId { get; } = Id<UserSocialGraph>.New();
        public UserSocialGraph(Guid id) : base(id)
        {

        }
    }
}