using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Common.MessageBroker
{
    public interface IMessageBrokerProducer
    {
        Task Produce<T>(string queue, T data);
    }

    public class Producer : IMessageBrokerProducer
    {
        public async Task Produce<T>(string queue, T data)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare(queue,
                false,
                false,
                false,
                null);

            var message = JsonConvert.SerializeObject(data);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish("",
                queue,
                null,
                body);
        }
    }
}