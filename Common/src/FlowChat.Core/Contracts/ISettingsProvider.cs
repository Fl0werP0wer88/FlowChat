namespace FlowChat.Core.Contracts;

public interface ISettingsProvider
{
    TSection GetSection<TSection>()
        where TSection : ISettingSection, new();

    TSection GetRequiredSection<TSection>()
        where TSection : ISettingSection, new();
}
