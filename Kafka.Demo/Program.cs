using Confluent.Kafka;
using Kafka.Public;
using Kafka.Public.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Kafka.Demo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((context, collection) =>
            {
                collection.AddHostedService<KafkaProducerHostedService>();
                collection.AddHostedService<KafkaConsumerHostedService>();
            });

    }
    public class KafkaProducerHostedService : IHostedService
    {
        private readonly ILogger<KafkaProducerHostedService> _logger;
        private readonly IProducer<Null, string> _producer;

        public KafkaProducerHostedService(ILogger<KafkaProducerHostedService> logger)
        {
            _logger = logger;
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            };
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            for (int i = 0; i < 100; i++)
            {
                var value = $"Hello World {i}";
                _logger.LogInformation(value);
                await _producer.ProduceAsync("demo", new Message<Null, string>()
                {
                    Value = value,
                }, cancellationToken);

                _producer.Flush(TimeSpan.FromSeconds(10));

            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _producer?.Dispose();
            return Task.CompletedTask;
        }
    }

    public class KafkaConsumerHostedService : IHostedService
    {
        private readonly ILogger<KafkaConsumerHostedService> _logger;
        private readonly IConsumer<Null, string> _consumer;

        public KafkaConsumerHostedService(ILogger<KafkaConsumerHostedService> logger)
        {
            _logger = logger;
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "demo",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            _consumer = new ConsumerBuilder<Null, string>(config).Build();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                _consumer.Subscribe("demo");
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = _consumer.Consume(cancellationToken);
                    _logger.LogInformation($"Received: {consumeResult.Message.Value}");
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _consumer?.Close();
            _consumer?.Dispose();
            return Task.CompletedTask;
        }
    }
}
