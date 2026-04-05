using FlowChat.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.Shared.Persistance;

public static class UtcDateTimeOffsetPropertyBuilderExtensions
{
    public static PropertyBuilder<UtcDateTimeOffset> HasUtcDateTimeOffsetConversion(
        this PropertyBuilder<UtcDateTimeOffset> propertyBuilder)
    {
        return propertyBuilder.HasConversion(
            value => value.Value,
            value => UtcDateTimeOffset.Create(value));
    }

    public static PropertyBuilder<UtcDateTimeOffset?> HasNullableUtcDateTimeOffsetConversion(
        this PropertyBuilder<UtcDateTimeOffset?> propertyBuilder)
    {
        return propertyBuilder.HasConversion(
            value => value == null ? (DateTimeOffset?)null : value.Value,
            value => value == null ? null : UtcDateTimeOffset.Create(value.Value));
    }
}
