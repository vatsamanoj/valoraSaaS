using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Infrastructure.BackgroundJobs;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly ProducerConfig _producerConfig;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger, IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        var bootstrap = config["Kafka:BootstrapServers"] ?? "localhost:9092";
        if (bootstrap.Contains("localhost")) bootstrap = bootstrap.Replace("localhost", "127.0.0.1");

        _producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrap
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure we don't block startup
        _logger.LogInformation("OutboxProcessor starting.");
        using var producer = new ProducerBuilder<string, string>(_producerConfig).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Console.WriteLine("[CONSOLE DEBUG] OutboxProcessor: Polling DB...");
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

                var messages = await dbContext.OutboxMessages
                        .Where(m => m.Status == "Pending")
                        .OrderBy(m => m.CreatedAt)
                        .Take(20)
                        .ToListAsync(stoppingToken);

                    if (messages.Any())
                    {
                        _logger.LogInformation("Found {Count} pending messages", messages.Count);
                        foreach (var message in messages)
                    {
                        try
                        {
                            await producer.ProduceAsync(message.Topic, new Message<string, string> 
                            { 
                                Key = message.TenantId.ToString(), 
                                Value = message.Payload 
                            }, stoppingToken);

                            message.Status = "Published";
                            message.ProcessedAt = DateTime.UtcNow;
                            _logger.LogInformation("Published message {MessageId} to {Topic}", message.Id, message.Topic);
                            // Console.WriteLine($"[CONSOLE DEBUG] OutboxProcessor: Published {message.Topic}");
                        }
                        catch (Exception ex)
                        {
                            message.Status = "Failed";
                            message.Error = ex.Message;
                            _logger.LogError(ex, "Failed to publish message {MessageId}", message.Id);
                        }
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                else
                {
                    await Task.Delay(1000, stoppingToken); // Wait if no messages
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxProcessor");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
