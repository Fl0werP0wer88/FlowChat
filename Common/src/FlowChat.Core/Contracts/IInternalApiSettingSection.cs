namespace FlowChat.Core.Contracts;

public interface IInternalApiSettingSection : ISettingSection
{
    string BaseUrl { get; }
    string? ApiKey { get; }
}
