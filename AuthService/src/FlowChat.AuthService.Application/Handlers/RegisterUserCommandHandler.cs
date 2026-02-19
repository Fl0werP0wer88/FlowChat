using FlowChat.AuthService.Application.Commands;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Responses;
using MediatR;

namespace FlowChat.AuthService.Application.Handlers;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserCommandResponse>
{
    private readonly IIdentityRepository _identityRepository;
    private readonly ITokenEncoder _tokenEncoder;
    private readonly IConfirmationLinkBuilder _confirmationLinkBuilder;
    private readonly IEmailService _emailService;
    private const string EMAIL_TOPIC = "Potwierdzenie rejestracji na FlowChat";
    private const string EMAIL_BODY = "Aby potwierdzić rejestracjie klikniji w link: ";
    private const string EMAIL_FOOTER = "Pozdrawiam Piotr Kwiatkowski";

    public RegisterUserCommandHandler(
        IIdentityRepository identityRepository,
        ITokenEncoder tokenEncoder,
        IConfirmationLinkBuilder confirmationLinkBuilder,
        IEmailService emailService)
    {
        _identityRepository = identityRepository;
        _tokenEncoder = tokenEncoder;
        _confirmationLinkBuilder = confirmationLinkBuilder;
        _emailService= emailService;
    }

    public async Task<RegisterUserCommandResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
         var guid = await _identityRepository.CreateUserAsync(request, cancellationToken);
         var confirmationToken = await _identityRepository.GenerateEmailConfirmationTokenAsync(guid, cancellationToken);
         var encodedToken = _tokenEncoder.EncodeForUrl(confirmationToken);
         var confirmationLink = _confirmationLinkBuilder.BuildEmailConfirmationLink(guid, encodedToken);
         var emailBody = $"{EMAIL_BODY}{Environment.NewLine}{Environment.NewLine}{confirmationLink}{Environment.NewLine}{Environment.NewLine}{EMAIL_FOOTER}";
         await _emailService.SendEmailAsync(request.Email,EMAIL_TOPIC,emailBody, false, cancellationToken);
         
         return new RegisterUserCommandResponse
         {
             Id = guid
         };
    }
}
