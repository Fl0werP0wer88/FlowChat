namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface ITokenEncoder
{
    string EncodeForUrl(string token);
    string DecodeFromUrl(string encodedToken);
}
