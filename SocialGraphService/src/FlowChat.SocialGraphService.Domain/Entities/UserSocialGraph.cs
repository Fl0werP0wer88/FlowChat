using FlowChat.SocialGraphService.Domain.Common;

namespace FlowChat.SocialGraphService.Domain.Entities
{
    public class UserSocialGraph : AggregateRootBase
    {
        public UserSocialGraph(Guid id) : base(id)
        {
            
        }
    }
}