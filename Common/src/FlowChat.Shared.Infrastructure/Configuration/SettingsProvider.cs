using FlowChat.Core.Contracts;
using Microsoft.Extensions.Configuration;

namespace FlowChat.Shared.Infrastructure.Configuration;

public sealed class SettingsProvider(IConfiguration configuration) : ISettingsProvider
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public TSection GetSection<TSection>()
        where TSection : ISettingSection, new()
    {
        var defaultSection = CreateSection<TSection>();
        var settings = _configuration.GetSection(defaultSection.SectionName).Get<TSection>() ?? defaultSection;

        return settings;
    }

    private TSection CreateSection<TSection>()
        where TSection : ISettingSection, new() => new();
}
