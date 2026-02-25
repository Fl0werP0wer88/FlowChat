using FlowChat.NotificationService.Application;
using FlowChat.NotificationService.Infrastructure;
using FlowChat.NotificationService.Persistence;
using FlowChat.NotificationService.Worker;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWorkerApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWorkerKafkaConsumer(builder.Configuration);
builder.Services.AddPersistenceServices(builder.Configuration);

var host = builder.Build();
host.Run();
