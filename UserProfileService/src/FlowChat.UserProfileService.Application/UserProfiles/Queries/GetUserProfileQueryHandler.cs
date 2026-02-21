using FlowChat.UserProfileService.Application.Contracts.Mapping;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using MediatR;

namespace FlowChat.UserProfileService.Application.UserProfiles.Queries;

public sealed class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto?>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IObjectMapper _mapper;

    public GetUserProfileQueryHandler(IUserProfileRepository userProfileRepository, IObjectMapper mapper)
    {
        _userProfileRepository = userProfileRepository;
        _mapper = mapper;
    }

    public async Task<UserProfileDto?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var entity = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);

        return entity is null ? null : _mapper.Map<UserProfileDto>(entity);
    }
}
