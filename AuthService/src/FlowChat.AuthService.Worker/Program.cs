using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.AddWolverineMessaging();
builder.Services.AddWorkerPersistenceServices(builder.Configuration);

await builder.Build().RunAsync();
