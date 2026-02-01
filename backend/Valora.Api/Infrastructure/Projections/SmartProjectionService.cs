using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Application.Schemas;
using Valora.Api.Application.Schemas.TemplateConfig;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Infrastructure.Projections;

/// <summary>
/// Service that orchestrates smart projection operations including index management,
/// query optimization, denormalization, and archival strategies.
/// </summary>
public class SmartProjectionService
{
    private readonly MongoDbContext _mongoDb;
    private readonly IndexManager _indexManager;
    private readonly ProjectionOptimizer _optimizer;
    private readonly ISchemaProvider _schemaProvider;
    private readonly ILogger<SmartProjectionService> _logger;

    // Cache for collection configurations
    private readonly Dictionary<string, SmartProjectionConfig?> _configCache = new();
    private readonly SemaphoreSlim _configCacheLock = new(1, 1);

    public SmartProjectionService(
        MongoDbContext mongoDb,
        IndexManager indexManager,
        ProjectionOptimizer optimizer,
        ISchemaProvider schemaProvider,
        ILogger<SmartProjectionService> logger)
    {
        _mongoDb = mongoDb;
        _indexManager = indexManager;
        _optimizer = optimizer;
        _schemaProvider = schemaProvider;
        _logger = logger;
    }

    /// <summary>
    /// Initializes smart projections for a collection based on its schema configuration.
    /// This should be called when a schema is registered or updated.
    /// </summary>
    public async Task InitializeCollectionAsync(
        string tenantId,
        string module,
        int version,
        string aggregateType,
        CancellationToken cancellationToken = default)
    {
        var collectionName = $"Entity_{aggregateType}";

        try
        {
            // Get schema and extract projection config
            var schema = await _schemaProvider.GetSchemaAsync(tenantId, module, cancellationToken);
            var config = schema?.SmartProjection;

            // Ensure indexes are created
            await _indexManager.EnsureIndexesAsync(collectionName, config, cancellationToken);

            // Run initial optimization analysis if auto-optimization is enabled
            if (config?.AutoOptimize == true)
            {
                var result = await _optimizer.AnalyzeAndOptimizeAsync(collectionName, config, cancellationToken);
                if (result.CreatedIndexes.Any())
                {
                    _logger.LogInformation(
                        "Auto-created {Count} indexes for {CollectionName}",
                        result.CreatedIndexes.Count, collectionName);
                }
            }

            // Cache the config
            await _configCacheLock.WaitAsync(cancellationToken);
            try
            {
                _configCache[collectionName] = config;
            }
            finally
            {
                _configCacheLock.Release();
            }

            _logger.LogInformation(
                "Initialized smart projections for {CollectionName} with config: {HasConfig}",
                collectionName, config != null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize smart projections for {CollectionName}", collectionName);
            throw;
        }
    }

    /// <summary>
    /// Processes a document before projection, applying denormalization and compression.
    /// </summary>
    public async Task<BsonDocument> ProcessDocumentAsync(
        string aggregateType,
        BsonDocument document,
        CancellationToken cancellationToken = default)
    {
        var collectionName = $"Entity_{aggregateType}";
        var config = await GetConfigAsync(collectionName, cancellationToken);

        if (config == null)
        {
            return document;
        }

        var processedDoc = document;

        // Apply denormalization
        if (config.Denormalizations.Any())
        {
            await _optimizer.ApplyDenormalizationAsync(
                collectionName, aggregateType, processedDoc, config.Denormalizations, cancellationToken);
        }

        // Apply compression to large fields
        if (config.Compression?.Enabled == true)
        {
            processedDoc = _optimizer.CompressLargeFields(processedDoc, config.Compression);
        }

        return processedDoc;
    }

    /// <summary>
    /// Records a query execution for pattern analysis.
    /// </summary>
    public void RecordQuery(
        string aggregateType,
        FilterDefinition<BsonDocument> filter,
        SortDefinition<BsonDocument>? sort = null,
        ProjectionDefinition<BsonDocument>? projection = null,
        long executionTimeMs = 0,
        long docsExamined = 0,
        long docsReturned = 0)
    {
        var collectionName = $"Entity_{aggregateType}";
        _optimizer.RecordQuery(collectionName, filter, sort, projection, executionTimeMs, docsExamined, docsReturned);
    }

    /// <summary>
    /// Runs optimization analysis on all initialized collections.
    /// This can be called periodically via a background job.
    /// </summary>
    public async Task<BatchOptimizationResult> RunBatchOptimizationAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new BatchOptimizationResult();

        await _configCacheLock.WaitAsync(cancellationToken);
        try
        {
            foreach (var (collectionName, config) in _configCache)
            {
                if (config?.AutoOptimize != true)
                {
                    continue;
                }

                try
                {
                    var optimizationResult = await _optimizer.AnalyzeAndOptimizeAsync(
                        collectionName, config, cancellationToken);
                    result.Results.Add(optimizationResult);

                    if (optimizationResult.CreatedIndexes.Any())
                    {
                        result.TotalIndexesCreated += optimizationResult.CreatedIndexes.Count;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch optimization failed for {CollectionName}", collectionName);
                    result.Errors.Add($"{collectionName}: {ex.Message}");
                }
            }
        }
        finally
        {
            _configCacheLock.Release();
        }

        result.CompletedAt = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// Gets optimization statistics for a collection.
    /// </summary>
    public async Task<CollectionOptimizationStats> GetStatsAsync(
        string aggregateType,
        CancellationToken cancellationToken = default)
    {
        var collectionName = $"Entity_{aggregateType}";
        var config = await GetConfigAsync(collectionName, cancellationToken);

        var stats = new CollectionOptimizationStats
        {
            CollectionName = collectionName,
            SmartProjectionEnabled = config != null,
            AutoOptimizeEnabled = config?.AutoOptimize == true
        };

        // Get index information
        try
        {
            stats.Indexes = await _indexManager.ListIndexesAsync(collectionName, cancellationToken);
            stats.IndexUsage = await _indexManager.GetIndexUsageStatsAsync(collectionName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get index stats for {CollectionName}", collectionName);
        }

        // Get query pattern stats
        stats.QueryPatterns = _optimizer.GetPatternStats(collectionName);

        return stats;
    }

    /// <summary>
    /// Archives old documents based on archival configuration.
    /// </summary>
    public async Task<ArchivalResult> ArchiveOldDocumentsAsync(
        string aggregateType,
        CancellationToken cancellationToken = default)
    {
        var collectionName = $"Entity_{aggregateType}";
        var config = await GetConfigAsync(collectionName, cancellationToken);

        if (config?.Archival?.Enabled != true)
        {
            return new ArchivalResult
            {
                CollectionName = collectionName,
                Archived = 0,
                Message = "Archival not enabled"
            };
        }

        var archivalConfig = config.Archival;
        var collection = _mongoDb.GetCollection<BsonDocument>(collectionName);

        // Calculate cutoff date
        var cutoffDate = DateTime.UtcNow.AddDays(-archivalConfig.ArchiveAfterDays);
        var ageField = archivalConfig.AgeField;

        // Build filter for old documents
        var filter = Builders<BsonDocument>.Filter.Lt(ageField, cutoffDate);

        var result = new ArchivalResult { CollectionName = collectionName };

        try
        {
            // Count documents to archive
            var count = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            result.Found = (int)count;

            if (count == 0)
            {
                result.Message = "No documents to archive";
                return result;
            }

            // Archive based on destination
            switch (archivalConfig.Destination)
            {
                case ArchiveDestination.SeparateCollection:
                    var archiveCollection = _mongoDb.GetCollection<BsonDocument>(archivalConfig.TargetName);
                    var documents = await collection.Find(filter).ToListAsync(cancellationToken);

                    if (documents.Any())
                    {
                        await archiveCollection.InsertManyAsync(documents, cancellationToken: cancellationToken);
                        result.Archived = documents.Count;
                    }
                    break;

                case ArchiveDestination.S3:
                case ArchiveDestination.ColdStorage:
                    // These would require additional implementation for external storage
                    _logger.LogWarning("Archival to {Destination} not yet implemented", archivalConfig.Destination);
                    result.Message = $"Archival to {archivalConfig.Destination} not yet implemented";
                    return result;
            }

            // Delete archived documents if configured
            if (archivalConfig.DeleteAfterArchive && result.Archived > 0)
            {
                var deleteResult = await collection.DeleteManyAsync(filter, cancellationToken);
                result.Deleted = (int)deleteResult.DeletedCount;
            }

            result.Message = $"Successfully archived {result.Archived} documents";
            _logger.LogInformation("Archived {Archived} documents from {CollectionName}",
                result.Archived, collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive documents from {CollectionName}", collectionName);
            result.Message = $"Error: {ex.Message}";
            result.Success = false;
        }

        return result;
    }

    /// <summary>
    /// Clears the configuration cache and forces reload on next access.
    /// </summary>
    public async Task ClearConfigCacheAsync(CancellationToken cancellationToken = default)
    {
        await _configCacheLock.WaitAsync(cancellationToken);
        try
        {
            _configCache.Clear();
            _logger.LogInformation("Cleared smart projection configuration cache");
        }
        finally
        {
            _configCacheLock.Release();
        }
    }

    /// <summary>
    /// Gets or creates the default smart projection configuration for a module.
    /// </summary>
    public SmartProjectionConfig GetDefaultConfig(string objectType = "Transaction")
    {
        var config = new SmartProjectionConfig
        {
            AutoOptimize = true,
            QueryPatternTracking = new QueryPatternConfig
            {
                Enabled = true,
                SampleRate = 0.1,
                AutoCreateIndexes = true,
                AutoSuggestDenormalizations = true,
                MinQueryCountForAutoIndex = 100,
                AnalysisWindowHours = 24
            }
        };

        // Add default indexes based on object type
        if (objectType == "Transaction")
        {
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_status_date",
                Fields = new Dictionary<string, int> { { "Status", 1 }, { "DocumentDate", -1 } },
                Type = IndexType.Compound
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_document_number",
                Fields = new Dictionary<string, int> { { "DocumentNumber", 1 } },
                Type = IndexType.Standard,
                IsUnique = true
            });
        }
        else // Master data
        {
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_code",
                Fields = new Dictionary<string, int> { { "Code", 1 } },
                Type = IndexType.Standard,
                IsUnique = true
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_name",
                Fields = new Dictionary<string, int> { { "Name", 1 } },
                Type = IndexType.Standard
            });
        }

        // Add common indexes
        config.Indexes.Add(new IndexConfig
        {
            Name = "idx_is_active",
            Fields = new Dictionary<string, int> { { "IsActive", 1 } },
            Type = IndexType.Standard
        });

        return config;
    }

    #region Private Helper Methods

    private async Task<SmartProjectionConfig?> GetConfigAsync(
        string collectionName,
        CancellationToken cancellationToken)
    {
        // Try cache first
        await _configCacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_configCache.TryGetValue(collectionName, out var cachedConfig))
            {
                return cachedConfig;
            }
        }
        finally
        {
            _configCacheLock.Release();
        }

        // Not in cache, try to get from schema
        try
        {
            // Extract aggregate type from collection name (Entity_Xyz -> Xyz)
            var aggregateType = collectionName.StartsWith("Entity_")
                ? collectionName[7..]
                : collectionName;

            // We need tenant/module info - this is a simplification
            // In practice, you'd need to look up the schema by aggregate type
            // For now, return null to use defaults
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get config for {CollectionName}", collectionName);
            return null;
        }
    }

    #endregion
}

/// <summary>
/// Result of a batch optimization run.
/// </summary>
public class BatchOptimizationResult
{
    public List<OptimizationResult> Results { get; set; } = new();
    public int TotalIndexesCreated { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Optimization statistics for a collection.
/// </summary>
public class CollectionOptimizationStats
{
    public string CollectionName { get; set; } = string.Empty;
    public bool SmartProjectionEnabled { get; set; }
    public bool AutoOptimizeEnabled { get; set; }
    public List<IndexInfo> Indexes { get; set; } = new();
    public List<IndexUsageInfo> IndexUsage { get; set; } = new();
    public QueryPatternStats QueryPatterns { get; set; } = new();
}

/// <summary>
/// Result of an archival operation.
/// </summary>
public class ArchivalResult
{
    public string CollectionName { get; set; } = string.Empty;
    public int Found { get; set; }
    public int Archived { get; set; }
    public int Deleted { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
}
