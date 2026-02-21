using FlowChat.SocialGraphService.Application;
using FlowChat.SocialGraphService.Infrastructure;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Worker;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWorkerApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWorkerKafkaConsumer(builder.Configuration);
builder.Services.AddPersistenceServices(builder.Configuration);

var host = builder.Build();
host.Run();
