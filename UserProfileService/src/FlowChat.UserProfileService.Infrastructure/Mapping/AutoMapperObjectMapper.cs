using AutoMapper;
using FlowChat.UserProfileService.Application.Contracts.Mapping;

namespace FlowChat.UserProfileService.Infrastructure.Mapping;

internal sealed class AutoMapperObjectMapper : IObjectMapper
{
    private readonly IMapper _mapper;

    public AutoMapperObjectMapper(IMapper mapper)
    {
        _mapper = mapper;
    }

    public TDestination Map<TDestination>(object source)
    {
        return _mapper.Map<TDestination>(source);
    }
}
