namespace FlowChat.Domain.Abstractions;

[AttributeUsage(AttributeTargets.Class)]
public class AggregateTypeAttribute(string aggregateType) : Attribute
{
    public string AggregateType { get; } = aggregateType;
}
