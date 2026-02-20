using FlowChat.UserProfileService.Application;
using FlowChat.UserProfileService.Infrastructure;
using FlowChat.UserProfileService.Persistence;
using FlowChat.UserProfileService.Worker;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWorkerApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWorkerKafkaConsumer(builder.Configuration);
builder.Services.AddPersistenceServices(builder.Configuration);

var host = builder.Build();
host.Run();
