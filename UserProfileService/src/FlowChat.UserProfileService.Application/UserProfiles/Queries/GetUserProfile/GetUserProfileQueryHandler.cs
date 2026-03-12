using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Mapping;
using FlowChat.UserProfileService.Application.Contracts.Persistence;

namespace FlowChat.UserProfileService.Application.UserProfiles.Queries.GetUserProfile;

public sealed class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IObjectMapper _mapper;

    public GetUserProfileQueryHandler(IUserProfileRepository userProfileRepository, IObjectMapper mapper)
    {
        _userProfileRepository = userProfileRepository;
        _mapper = mapper;
    }

    public async Task<Result<UserProfileDto, IDomainError>> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (entity is null)
        {
            return Result.Failure<UserProfileDto, IDomainError>(
                DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        return Result.Success<UserProfileDto, IDomainError>(_mapper.Map<UserProfileDto>(entity));
    }
}
