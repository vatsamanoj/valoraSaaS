using Confluent.Kafka;
using System;

class Program
{
    static void Main(string[] args)
    {
        var config = new AdminClientConfig
        {
            BootstrapServers = "localhost:9092"
        };

        using (var adminClient = new AdminClientBuilder(config).Build())
        {
            try
            {
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                Console.WriteLine($"Connected to Kafka cluster: {metadata.OriginatingBrokerId}");
                Console.WriteLine("Brokers:");
                foreach (var broker in metadata.Brokers)
                {
                    Console.WriteLine($"  Broker: {broker.BrokerId} {broker.Host}:{broker.Port}");
                }
                Console.WriteLine("Topics:");
                foreach (var topic in metadata.Topics)
                {
                    Console.WriteLine($"  Topic: {topic.Topic} (Partitions: {topic.Partitions.Count})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to Kafka: {ex.Message}");
            }
        }
    }
}
