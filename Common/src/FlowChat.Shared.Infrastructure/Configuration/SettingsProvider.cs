using FlowChat.Core.Contracts;
using Microsoft.Extensions.Configuration;

namespace FlowChat.Shared.Infrastructure.Configuration;

public sealed class SettingsProvider(IConfiguration configuration) : ISettingsProvider
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public TSection GetSection<TSection>()
        where TSection : ISettingSection, new()
    {
        var section = CreateSection<TSection>();
        return _configuration.GetSection(section.SectionName).Get<TSection>()
            ?? section;
    }

    private static TSection CreateSection<TSection>()
        where TSection : ISettingSection, new() => new();
}
