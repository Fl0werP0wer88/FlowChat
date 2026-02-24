namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IOutboxRepositoryOptions<TEvent>
{
    string Topic { get; set; }
    Func<TEvent, string> KeySelector { get; }
}
