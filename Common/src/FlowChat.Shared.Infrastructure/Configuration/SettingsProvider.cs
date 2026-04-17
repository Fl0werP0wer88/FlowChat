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
        return _configuration.GetSection(defaultSection.SectionName).Get<TSection>() ?? defaultSection;
    }

    public TSection GetRequiredSection<TSection>()
        where TSection : ISettingSection, new()
    {
        var sectionName = CreateSection<TSection>().SectionName;
        return _configuration.GetSection(sectionName).Get<TSection>()
            ?? throw new InvalidOperationException($"Missing configuration section: {sectionName}.");
    }

    private static TSection CreateSection<TSection>()
        where TSection : ISettingSection, new() => new();
}
