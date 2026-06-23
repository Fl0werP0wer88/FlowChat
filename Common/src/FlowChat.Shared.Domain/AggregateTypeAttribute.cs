namespace FlowChat.Shared.Domain;

[AttributeUsage(AttributeTargets.Class)]
public class AggregateTypeAttribute(string aggregateType) : Attribute
{
    public string AggregateType { get; } = aggregateType;
}

