using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkerPersistenceServices(builder.Configuration);
builder.Services.AddWorkerMassTransit(builder.Configuration);

await builder.Build().RunAsync();
