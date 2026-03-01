using FlowChat.AuthService.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkerPersistenceServices(builder.Configuration);
builder.Services.AddWorkerSilverbackMessaging(builder.Configuration);

await builder.Build().RunAsync();
