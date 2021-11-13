using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Common.MessageBroker
{
    public interface IMessageBrokerProducer
    {
        void Produce<T>(string exchange, T data);
    }

    public class Producer : IMessageBrokerProducer
    {
        public void Produce<T>(string exchange, T data)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange, "direct");

            var message = JsonConvert.SerializeObject(data);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange, "", null, body);
        }
    }
}