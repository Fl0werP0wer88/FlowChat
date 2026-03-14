using Microsoft.AspNetCore.Builder;

namespace FlowChat.API.Abstractions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseFlowChatGlobalExceptionHandling(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

        return app;
    }
}
