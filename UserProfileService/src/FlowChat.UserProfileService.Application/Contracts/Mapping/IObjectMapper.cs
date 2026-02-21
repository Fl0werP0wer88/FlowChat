namespace FlowChat.UserProfileService.Application.Contracts.Mapping;

public interface IObjectMapper
{
    TDestination Map<TDestination>(object source);
}
