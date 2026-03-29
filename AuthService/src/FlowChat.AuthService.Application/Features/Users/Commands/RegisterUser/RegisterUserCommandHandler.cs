using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Domain.Entities;
using CSharpFunctionalExtensions;
using FlowChat.Shared.Domain;

namespace FlowChat.AuthService.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : CommandHandlerBase<RegisterUserCommand, RegisterUserCommandResponse>
{
    private const string UserCreationErrorPrefix = "User creation failed:";
    private static readonly HashSet<string> ConflictErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "DuplicateUserName",
        "DuplicateEmail"
    };

    private readonly IIdentityRepository _identityRepository;
    private Identity? _domainUser;
    public RegisterUserCommandHandler(
        IIdentityRepository identityRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _identityRepository = identityRepository;
    }

    protected override async Task<FlowChatResult<RegisterUserCommandResponse>> ExecuteAsync(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        _domainUser = Identity.Create(Guid.NewGuid(), request.UserName, request.Email, request.PhoneNumber);
        Guid guid;
        try
        {
            guid = await _identityRepository.CreateUserAsync(_domainUser, request.Password, cancellationToken);
        }
        catch (InvalidOperationException exception) when (TryMapUserCreationError(exception, out var domainError))
        {
            return FlowChatResult<RegisterUserCommandResponse>.Failure(domainError);
        }

        return FlowChatResult<RegisterUserCommandResponse>.Success(
            new RegisterUserCommandResponse
            {
                Id = guid
            });
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<RegisterUserCommandResponse> result)
    {
        return _domainUser;
    }

    private static bool TryMapUserCreationError(InvalidOperationException exception, out IDomainError domainError)
    {
        if (!exception.Message.StartsWith(UserCreationErrorPrefix, StringComparison.Ordinal))
        {
            domainError = null!;
            return false;
        }

        var rawErrors = exception.Message[UserCreationErrorPrefix.Length..]
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var parsedErrors = rawErrors
            .Select(ParseIdentityError)
            .Where(error => error is not null)
            .Cast<(string Code, string Description)>()
            .ToArray();

        var errorDescriptions = parsedErrors.Length == 0
            ? ["User creation failed."]
            : parsedErrors.Select(error => error.Description).ToList();

        domainError = parsedErrors.Any(error => ConflictErrorCodes.Contains(error.Code))
            ? DomainError.Conflict("User with the provided username or email already exists.") with { Errors = errorDescriptions }
            : DomainError.Validation("User registration validation failed.", errorDescriptions);

        return true;
    }

    private static (string Code, string Description)? ParseIdentityError(string rawError)
    {
        var separatorIndex = rawError.IndexOf(':');
        if (separatorIndex < 0)
        {
            var trimmed = rawError.Trim();
            return string.IsNullOrWhiteSpace(trimmed)
                ? null
                : (string.Empty, trimmed);
        }

        var code = rawError[..separatorIndex].Trim();
        var description = rawError[(separatorIndex + 1)..].Trim();

        return (code, description);
    }
}

