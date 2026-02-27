using FlowChat.SocialGraphService.Domain.Common.Contracts;

namespace FlowChat.SocialGraphService.Domain.Entities
{
    public class UserSocialGraph : AggregateRootBase
    {
        public UserSocialGraph(Guid id) : base(id)
        {

        }
    }
}