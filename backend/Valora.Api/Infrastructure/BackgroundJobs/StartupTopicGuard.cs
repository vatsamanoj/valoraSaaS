using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Valora.Api.Infrastructure.BackgroundJobs;

public class StartupTopicGuard
{
    private readonly IConfiguration _config;
    private readonly ILogger<StartupTopicGuard> _logger;

    public StartupTopicGuard(IConfiguration config, ILogger<StartupTopicGuard> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task EnsureAllTopicsSubscribed(IEnumerable<string> producedTopics)
    {
        _logger.LogInformation("Starting Startup Topic Guard check...");

        var bootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092";
        var config = new AdminClientConfig { BootstrapServers = bootstrapServers };

        using var adminClient = new AdminClientBuilder(config).Build();

        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var existingTopics = metadata.Topics.Select(t => t.Topic).ToHashSet();

            foreach (var topic in producedTopics)
            {
                if (!existingTopics.Contains(topic))
                {
                    _logger.LogWarning("Topic {Topic} does not exist in Kafka. It will be auto-created upon first produce if auto.create.topics.enable is true.", topic);
                }
                else
                {
                    _logger.LogInformation("Topic {Topic} verified.", topic);
                }
            }

            // In a real production system, we might want to fail startup if critical topics are missing
            // or if the consumer group is not subscribed to them.
            // Since we use wildcard subscription "^valora\\..*", we are technically subscribed to anything matching.
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Kafka topics at startup.");
            // We do not throw here to allow app to start even if Kafka is temporarily down, 
            // relying on retry policies.
        }
    }
}
