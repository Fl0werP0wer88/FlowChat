using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.ChatService.IntegrationTests.Persistence;

public sealed class PersistenceServiceRegistrationTests
{
    [Fact]
    public void AddApiPersistenceServices_RegistersConversationRepositories()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ChatDb"] = "Host=localhost;Port=5432;Database=flowchat_chat_test_db;Username=test;Password=test"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddApiPersistenceServices(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var duetReadRepository = scope.ServiceProvider.GetService<IDuetConversationReadRepository>();
        var participantReadRepository = scope.ServiceProvider.GetService<IConversationParticipantReadRepository>();
        var conversationWriteRepository = scope.ServiceProvider.GetService<IConversationV2WriteRepository>();
        var sequenceRepository = scope.ServiceProvider.GetService<IConversationMessageSequenceRepositoryV2>();
        duetReadRepository.Should().NotBeNull();
        conversationWriteRepository.Should().NotBeNull();
        participantReadRepository.Should().NotBeNull();
        sequenceRepository.Should().NotBeNull();
    }
}
