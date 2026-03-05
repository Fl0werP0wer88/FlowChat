namespace FlowChat.Domain.Abstractions;

public sealed record Id<TModel> : IId, IComparable, IComparable<IId>, IComparable<Guid>, IEquatable<IId>
{
    public Guid Value { get; init; }

    public Id(Guid value)
    {
        Value = value != Guid.Empty
            ? value
            : throw new ArgumentException("value cannot be empty.", nameof(value));
    }

    public Id() : this(Guid.NewGuid())
    {
    }

    public static Id<TModel> New() => new(Guid.NewGuid());

    public static Id<TModel> FromId<TNewModel>(Id<TNewModel> id) => new(id.Value);

    public static Id<TModel> FromGuid(Guid id) => new(id);

    public static Id<TModel> FromString(string id) => new(Guid.Parse(id));

    public static implicit operator Guid?(Id<TModel>? id) => id?.Value;

    public static implicit operator Guid(Id<TModel> id) => id.Value;

    public static implicit operator Id<TModel>(Guid id) => new(id);

    public int CompareTo(object? obj)
    {
        if (obj is IId otherId)
        {
            return CompareTo(otherId);
        }

        if (obj is Guid otherGuid)
        {
            return CompareTo(otherGuid);
        }

        if (obj == null)
        {
            return 1;
        }

        throw new ArgumentException("Object must be of type IId or Guid.", nameof(obj));
    }

    public int CompareTo(IId? other) => other?.Value.CompareTo(Value) ?? 1;

    public int CompareTo(Guid other) => Value.CompareTo(other);

    public bool Equals(IId? other) => other?.Value == Value;

    public override string ToString() => Value.ToString();
}
