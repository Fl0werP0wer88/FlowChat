using AutoMapper;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class UserSocialGraphRepository : IUserSocialGraphRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;

    public UserSocialGraphRepository(AppDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<UserSocialGraph?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.UserSocialGraphs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        return entity is null ? null : _mapper.Map<UserSocialGraph>(entity);
    }

    public async Task<UserSocialGraph> AddAsync(UserSocialGraph entity, CancellationToken cancellationToken = default)
    {
        var persistenceEntity = _mapper.Map<UserSocialGraphEntity>(entity);

        await _dbContext.UserSocialGraphs.AddAsync(persistenceEntity, cancellationToken);

        return entity;
    }
}
