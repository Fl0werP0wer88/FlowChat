using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.API.Features.User.Public.RegisterUser;

public sealed class RegisterUserResponse : IServiceOutput
{
    public Guid Id { get; set; }
}
