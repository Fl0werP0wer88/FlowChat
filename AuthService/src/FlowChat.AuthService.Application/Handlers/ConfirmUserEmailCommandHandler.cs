using FlowChat.AuthService.Application.Commands;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Responses;
using MediatR;

namespace FlowChat.AuthService.Application.Handlers;

public class ConfirmUserEmailCommandHandler : IRequestHandler<ConfirmUserEmailCommand, ConfirmUserEmailCommandResponse>
{
    private readonly IIdentityRepository _identityRepository;
    private readonly ITokenEncoder _tokenEncoder;

    public ConfirmUserEmailCommandHandler(IIdentityRepository identityRepository, ITokenEncoder tokenEncoder)
    {
        _identityRepository = identityRepository;
        _tokenEncoder = tokenEncoder;
    }

    public async Task<ConfirmUserEmailCommandResponse> Handle(ConfirmUserEmailCommand request, CancellationToken cancellationToken)
    {
        string decodedToken;
        try
        {
            decodedToken = _tokenEncoder.DecodeFromUrl(request.Token);
        }
        catch (FormatException)
        {
            return new ConfirmUserEmailCommandResponse { IsSuccess = false };
        }
        catch (ArgumentException)
        {
            return new ConfirmUserEmailCommandResponse { IsSuccess = false };
        }

        var isConfirmed = await _identityRepository.ConfirmEmailAsync(request.UserId, decodedToken, cancellationToken);

        return new ConfirmUserEmailCommandResponse
        {
            IsSuccess = isConfirmed
        };
    }
}
