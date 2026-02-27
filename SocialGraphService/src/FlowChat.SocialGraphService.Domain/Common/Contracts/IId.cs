namespace FlowChat.SocialGraphService.Domain.Common.Contracts;

public interface IId : IComparable, IComparable<IId>, IComparable<Guid>, IEquatable<IId>
{
    Guid Value { get; }
}