namespace FlowChat.Core.Contracts;

public interface ISettingsProvider
{
    TSection GetSection<TSection>()
        where TSection : ISettingSection, new();
}
