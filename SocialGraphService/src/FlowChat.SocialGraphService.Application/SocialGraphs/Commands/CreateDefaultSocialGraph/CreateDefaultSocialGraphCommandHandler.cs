using FlowChat.Domain.Abstractions;
using FlowChat.Application.Abstractions;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.SocialGraphs;
using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Application.SocialGraphs.Commands.CreateDefaultSocialGraph;

public sealed class CreateDefaultSocialGraphCommandHandler
    : CommandHandlerBase<CreateDefaultSocialGraphCommand, SocialGraphDto>
{
    private readonly IUserSocialGraphRepository _userSocialGraphRepository;
    private UserSocialGraph? _aggregateRoot;

    public CreateDefaultSocialGraphCommandHandler(
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork,
        IUserSocialGraphRepository userSocialGraphRepository)
        : base(domainEventDispatcher, unitOfWork)
    {
        _userSocialGraphRepository = userSocialGraphRepository;
    }

    protected override async Task<Result<SocialGraphDto, IDomainError>> ExecuteAsync(
        CreateDefaultSocialGraphCommand request,
        CancellationToken cancellationToken)
    {
        _aggregateRoot = null;

        if (request.UserId == Guid.Empty)
        {
            return Result.Failure<SocialGraphDto, IDomainError>(
                DomainError.BadRequest("UserId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Login))
        {
            return Result.Failure<SocialGraphDto, IDomainError>(
                DomainError.BadRequest("Login is required."));
        }

        var existingSocialGraph = await _userSocialGraphRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken);

        if (existingSocialGraph is not null)
        {
            return Result.Failure<SocialGraphDto, IDomainError>(
                DomainError.Conflict($"User social graph for user '{request.UserId}' already exists."));
        }

        var socialGraph = UserSocialGraph.Create(
            request.Login,
            request.UserId,
            request.FirstName,
            request.LastName,
            email: request.Email);

        _aggregateRoot = socialGraph;

        await _userSocialGraphRepository.AddAsync(socialGraph, cancellationToken);

        var response = new SocialGraphDto(
            socialGraph.Id.Value,
            socialGraph.UserId,
            socialGraph.Login,
            socialGraph.FirstName,
            socialGraph.LastName,
            socialGraph.PhoneNumber,
            socialGraph.Email,
            socialGraph.IsPhoneVisible,
            socialGraph.IsEmailVisible);

        return Result.Success<SocialGraphDto, IDomainError>(response);
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<SocialGraphDto, IDomainError> result)
    {
        return result.IsSuccess ? _aggregateRoot : null;
    }
}
