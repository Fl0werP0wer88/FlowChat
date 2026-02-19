using System.Text;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

namespace FlowChat.AuthService.Infrastructure.Services;

public class Base64UrlTokenEncoder : ITokenEncoder
{
    public string EncodeForUrl(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)); 
        var decoded = this.DecodeFromUrl(encoded);

        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)); 
    }

    public string DecodeFromUrl(string encodedToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encodedToken);
        return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
    }
}
