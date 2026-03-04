namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IKafkaProducerOptions
{
    string BootstrapServers { get; set; }
    string Topic { get; set; }
}

public interface IKafkaProducerOptions<TEvent> : IKafkaProducerOptions
{ }
