namespace FlowChat.Domain.Abstractions;

public interface IId : IComparable, IComparable<IId>, IComparable<Guid>, IEquatable<IId>
{
    Guid Value { get; }
}
