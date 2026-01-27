using Confluent.Kafka;
using System;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        var config = new AdminClientConfig
        {
            BootstrapServers = "127.0.0.1:9092", // Force IPv4
            SocketTimeoutMs = 5000
        };

        Console.WriteLine($"Connecting to Kafka at {config.BootstrapServers}...");

        try
        {
            using (var adminClient = new AdminClientBuilder(config).Build())
            {
                // Try to get metadata which requires a connection
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
                
                Console.WriteLine("SUCCESS: Connected to Kafka!");
                Console.WriteLine($"Originating Broker: {metadata.OriginatingBrokerName} (Id: {metadata.OriginatingBrokerId})");
                Console.WriteLine($"Brokers: {metadata.Brokers.Count}");
                foreach (var broker in metadata.Brokers)
                {
                    Console.WriteLine($" - {broker.Host}:{broker.Port} (Id: {broker.BrokerId})");
                }
                
                Console.WriteLine($"Topics: {metadata.Topics.Count}");
                foreach (var topic in metadata.Topics)
                {
                    Console.WriteLine($" - {topic.Topic} (Partitions: {topic.Partitions.Count})");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: Failed to connect to Kafka.");
            Console.WriteLine(ex.Message);
        }
    }
}
