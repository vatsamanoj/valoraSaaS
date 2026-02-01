using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Application.Schemas.TemplateConfig;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Infrastructure.Projections;

/// <summary>
/// Manages MongoDB indexes for projection collections.
/// Handles index creation, monitoring, and optimization based on template configuration.
/// </summary>
public class IndexManager
{
    private readonly MongoDbContext _mongoDb;
    private readonly ILogger<IndexManager> _logger;
    private readonly Dictionary<string, HashSet<string>> _createdIndexes = new();

    public IndexManager(MongoDbContext mongoDb, ILogger<IndexManager> logger)
    {
        _mongoDb = mongoDb;
        _logger = logger;
    }

    /// <summary>
    /// Ensures indexes are created for a collection based on SmartProjectionConfig.
    /// </summary>
    public async Task EnsureIndexesAsync(
        string collectionName,
        SmartProjectionConfig? config,
        CancellationToken cancellationToken = default)
    {
        var collection = _mongoDb.GetCollection<BsonDocument>(collectionName);

        // Always ensure base indexes
        await EnsureBaseIndexesAsync(collection, cancellationToken);

        if (config == null)
        {
            _logger.LogInformation("No SmartProjectionConfig provided for {CollectionName}, using base indexes only", collectionName);
            return;
        }

        // Apply configured indexes
        foreach (var indexConfig in config.Indexes)
        {
            try
            {
                await CreateIndexFromConfigAsync(collection, indexConfig, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create index {IndexName} on {CollectionName}",
                    indexConfig.Name, collectionName);
            }
        }

        // Apply TTL if configured
        if (config.TtlDays.HasValue)
        {
            await EnsureTtlIndexAsync(collection, config.TtlDays.Value, cancellationToken);
        }

        // Apply document validation if configured
        if (config.Validation?.JsonSchema != null)
        {
            await ApplyDocumentValidationAsync(collection, config.Validation, cancellationToken);
        }
    }

    /// <summary>
    /// Ensures base indexes that should exist on all projection collections.
    /// </summary>
    private async Task EnsureBaseIndexesAsync(
        IMongoCollection<BsonDocument> collection,
        CancellationToken cancellationToken)
    {
        var collectionName = collection.CollectionNamespace.CollectionName;

        // TenantId index (essential for multi-tenancy)
        var tenantIdIndexName = "idx_tenantId";
        if (!IsIndexCreated(collectionName, tenantIdIndexName))
        {
            var tenantIdIndex = Builders<BsonDocument>.IndexKeys.Ascending("TenantId");
            await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(tenantIdIndex, new CreateIndexOptions
                {
                    Name = tenantIdIndexName,
                    Background = true
                }),
                cancellationToken: cancellationToken);
            MarkIndexCreated(collectionName, tenantIdIndexName);
            _logger.LogDebug("Created base index {IndexName} on {CollectionName}", tenantIdIndexName, collectionName);
        }

        // _projectedAt index for time-based queries
        var projectedAtIndexName = "idx_projectedAt";
        if (!IsIndexCreated(collectionName, projectedAtIndexName))
        {
            var projectedAtIndex = Builders<BsonDocument>.IndexKeys.Descending("_projectedAt");
            await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(projectedAtIndex, new CreateIndexOptions
                {
                    Name = projectedAtIndexName,
                    Background = true
                }),
                cancellationToken: cancellationToken);
            MarkIndexCreated(collectionName, projectedAtIndexName);
            _logger.LogDebug("Created base index {IndexName} on {CollectionName}", projectedAtIndexName, collectionName);
        }

        // Compound index for tenant + projectedAt (common query pattern)
        var tenantProjectedAtIndexName = "idx_tenant_projectedAt";
        if (!IsIndexCreated(collectionName, tenantProjectedAtIndexName))
        {
            var compoundIndex = Builders<BsonDocument>.IndexKeys
                .Ascending("TenantId")
                .Descending("_projectedAt");
            await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(compoundIndex, new CreateIndexOptions
                {
                    Name = tenantProjectedAtIndexName,
                    Background = true
                }),
                cancellationToken: cancellationToken);
            MarkIndexCreated(collectionName, tenantProjectedAtIndexName);
            _logger.LogDebug("Created base index {IndexName} on {CollectionName}", tenantProjectedAtIndexName, collectionName);
        }
    }

    /// <summary>
    /// Creates an index from IndexConfig specification.
    /// </summary>
    private async Task CreateIndexFromConfigAsync(
        IMongoCollection<BsonDocument> collection,
        IndexConfig config,
        CancellationToken cancellationToken)
    {
        var collectionName = collection.CollectionNamespace.CollectionName;

        if (IsIndexCreated(collectionName, config.Name))
        {
            _logger.LogDebug("Index {IndexName} already exists on {CollectionName}", config.Name, collectionName);
            return;
        }

        IndexKeysDefinition<BsonDocument> keysDefinition;

        switch (config.Type)
        {
            case IndexType.Text:
                keysDefinition = CreateTextIndexKeys(config.Fields);
                break;

            case IndexType.Hashed:
                keysDefinition = CreateHashedIndexKeys(config.Fields);
                break;

            case IndexType.Wildcard:
                keysDefinition = Builders<BsonDocument>.IndexKeys.Ascending("$**");
                break;

            case IndexType.Compound:
            case IndexType.Standard:
            default:
                keysDefinition = CreateStandardIndexKeys(config.Fields);
                break;
        }

        var options = new CreateIndexOptions
        {
            Name = config.Name,
            Unique = config.IsUnique,
            Sparse = config.IsSparse,
            Background = true
        };

        // Apply collation if specified
        if (config.Collation != null)
        {
            options.Collation = CreateMongoCollation(config.Collation);
        }

        // Apply partial filter expression if specified
        if (!string.IsNullOrEmpty(config.PartialFilterExpression))
        {
            // Note: PartialFilterExpression is set via the keys definition in MongoDB C# Driver
            // We'll create a filtered index using a different approach
            _logger.LogDebug("Partial filter expression specified but requires manual index creation for {IndexName}", config.Name);
        }

        // Apply TTL if specified
        if (config.ExpireAfterSeconds.HasValue)
        {
            options.ExpireAfter = TimeSpan.FromSeconds(config.ExpireAfterSeconds.Value);
        }

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(keysDefinition, options),
            cancellationToken: cancellationToken);

        MarkIndexCreated(collectionName, config.Name);
        _logger.LogInformation("Created {IndexType} index {IndexName} on {CollectionName}",
            config.Type, config.Name, collectionName);
    }

    /// <summary>
    /// Creates standard/compound index keys from field configuration.
    /// </summary>
    private IndexKeysDefinition<BsonDocument> CreateStandardIndexKeys(Dictionary<string, int> fields)
    {
        IndexKeysDefinition<BsonDocument>? keys = null;

        foreach (var field in fields)
        {
            IndexKeysDefinition<BsonDocument> fieldKey = field.Value == 1
                ? Builders<BsonDocument>.IndexKeys.Ascending(field.Key)
                : Builders<BsonDocument>.IndexKeys.Descending(field.Key);

            keys = keys == null ? fieldKey : Builders<BsonDocument>.IndexKeys.Combine(keys, fieldKey);
        }

        return keys ?? Builders<BsonDocument>.IndexKeys.Ascending("_id");
    }

    /// <summary>
    /// Creates text index keys from field configuration.
    /// </summary>
    private IndexKeysDefinition<BsonDocument> CreateTextIndexKeys(Dictionary<string, int> fields)
    {
        IndexKeysDefinition<BsonDocument>? keys = null;

        foreach (var field in fields)
        {
            var fieldKey = Builders<BsonDocument>.IndexKeys.Text(field.Key);
            keys = keys == null ? fieldKey : Builders<BsonDocument>.IndexKeys.Combine(keys, fieldKey);
        }

        return keys ?? Builders<BsonDocument>.IndexKeys.Text("$**");
    }

    /// <summary>
    /// Creates hashed index keys from field configuration.
    /// </summary>
    private IndexKeysDefinition<BsonDocument> CreateHashedIndexKeys(Dictionary<string, int> fields)
    {
        var field = fields.FirstOrDefault();
        if (string.IsNullOrEmpty(field.Key))
        {
            return Builders<BsonDocument>.IndexKeys.Hashed("_id");
        }

        return Builders<BsonDocument>.IndexKeys.Hashed(field.Key);
    }

    /// <summary>
    /// Ensures a TTL index exists for automatic document expiration.
    /// </summary>
    private async Task EnsureTtlIndexAsync(
        IMongoCollection<BsonDocument> collection,
        int ttlDays,
        CancellationToken cancellationToken)
    {
        var collectionName = collection.CollectionNamespace.CollectionName;
        var indexName = "idx_ttl";

        if (IsIndexCreated(collectionName, indexName))
        {
            return;
        }

        var ttlIndex = Builders<BsonDocument>.IndexKeys.Ascending("_projectedAt");
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(ttlIndex, new CreateIndexOptions
            {
                Name = indexName,
                ExpireAfter = TimeSpan.FromDays(ttlDays),
                Background = true
            }),
            cancellationToken: cancellationToken);

        MarkIndexCreated(collectionName, indexName);
        _logger.LogInformation("Created TTL index on {CollectionName} with {TtlDays} days expiration",
            collectionName, ttlDays);
    }

    /// <summary>
    /// Applies document validation rules to the collection.
    /// </summary>
    private async Task ApplyDocumentValidationAsync(
        IMongoCollection<BsonDocument> collection,
        DocumentValidationConfig config,
        CancellationToken cancellationToken)
    {
        if (config.JsonSchema == null)
        {
            return;
        }

        var collectionName = collection.CollectionNamespace.CollectionName;
        var database = _mongoDb.Database;

        var validationLevel = config.Level switch
        {
            ValidationLevel.Off => ValidationLevel.Off,
            ValidationLevel.Moderate => ValidationLevel.Moderate,
            _ => ValidationLevel.Strict
        };

        var validationAction = config.Action switch
        {
            ValidationAction.Warn => ValidationAction.Warn,
            _ => ValidationAction.Error
        };

        var validator = new BsonDocument("$jsonSchema", config.JsonSchema.ToBsonDocument());

        var command = new BsonDocument
        {
            { "collMod", collectionName },
            { "validator", validator },
            { "validationLevel", validationLevel.ToString().ToLowerInvariant() },
            { "validationAction", validationAction.ToString().ToLowerInvariant() }
        };

        try
        {
            await database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
            _logger.LogInformation("Applied document validation to {CollectionName}", collectionName);
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("ns does not exist"))
        {
            // Collection doesn't exist yet, validation will be applied on first document insert
            _logger.LogDebug("Collection {CollectionName} does not exist yet, skipping validation setup", collectionName);
        }
    }

    /// <summary>
    /// Drops an index by name.
    /// </summary>
    public async Task DropIndexAsync(
        string collectionName,
        string indexName,
        CancellationToken cancellationToken = default)
    {
        var collection = _mongoDb.GetCollection<BsonDocument>(collectionName);
        await collection.Indexes.DropOneAsync(indexName, cancellationToken);

        if (_createdIndexes.TryGetValue(collectionName, out var indexes))
        {
            indexes.Remove(indexName);
        }

        _logger.LogInformation("Dropped index {IndexName} from {CollectionName}", indexName, collectionName);
    }

    /// <summary>
    /// Lists all indexes on a collection.
    /// </summary>
    public async Task<List<IndexInfo>> ListIndexesAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        var collection = _mongoDb.GetCollection<BsonDocument>(collectionName);
        var indexes = await collection.Indexes.ListAsync(cancellationToken);
        var indexList = await indexes.ToListAsync(cancellationToken);

        return indexList.Select(doc => new IndexInfo
        {
            Name = doc["name"].AsString,
            Keys = doc["key"].AsBsonDocument.ToDictionary(
                k => k.Name,
                k => k.Value.IsInt32 ? k.Value.AsInt32 : (int)k.Value.AsInt64),
            IsUnique = doc.Contains("unique") && doc["unique"].AsBoolean,
            IsSparse = doc.Contains("sparse") && doc["sparse"].AsBoolean
        }).ToList();
    }

    /// <summary>
    /// Gets index usage statistics.
    /// </summary>
    public async Task<List<IndexUsageInfo>> GetIndexUsageStatsAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        var database = _mongoDb.Database;
        var command = new BsonDocument("collStats", collectionName);

        try
        {
            var stats = await database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            if (!stats.Contains("indexDetails"))
            {
                return new List<IndexUsageInfo>();
            }

            var indexDetails = stats["indexDetails"].AsBsonDocument;
            var usageStats = new List<IndexUsageInfo>();

            foreach (var index in indexDetails)
            {
                var indexDoc = index.Value.AsBsonDocument;
                usageStats.Add(new IndexUsageInfo
                {
                    IndexName = index.Name,
                    Accesses = indexDoc.Contains("accesses")
                        ? (indexDoc["accesses"].IsInt32
                            ? indexDoc["accesses"].AsInt32
                            : (int)indexDoc["accesses"].AsInt64)
                        : 0,
                    Since = indexDoc.Contains("since")
                        ? indexDoc["since"].ToUniversalTime()
                        : DateTime.MinValue
                });
            }

            return usageStats;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get index usage stats for {CollectionName}", collectionName);
            return new List<IndexUsageInfo>();
        }
    }

    /// <summary>
    /// Analyzes query patterns and suggests missing indexes.
    /// </summary>
    public async Task<List<IndexSuggestion>> SuggestIndexesAsync(
        string collectionName,
        List<DetectedQueryPattern> patterns,
        CancellationToken cancellationToken = default)
    {
        var suggestions = new List<IndexSuggestion>();
        var existingIndexes = await ListIndexesAsync(collectionName, cancellationToken);

        foreach (var pattern in patterns.Where(p => !p.IndexCreated && p.ExecutionCount > 100))
        {
            // Check if there's already an index that covers this pattern
            var hasCoveringIndex = existingIndexes.Any(idx =>
                pattern.FilterFields.All(f => idx.Keys.ContainsKey(f)));

            if (hasCoveringIndex)
            {
                pattern.IndexCreated = true;
                continue;
            }

            // Suggest a compound index
            var suggestedFields = new Dictionary<string, int>();

            // Equality fields first (high selectivity)
            foreach (var field in pattern.FilterFields)
            {
                suggestedFields[field] = 1;
            }

            // Sort fields next
            foreach (var field in pattern.SortFields)
            {
                if (!suggestedFields.ContainsKey(field))
                {
                    suggestedFields[field] = 1;
                }
            }

            if (suggestedFields.Any())
            {
                suggestions.Add(new IndexSuggestion
                {
                    PatternHash = pattern.PatternHash,
                    SuggestedIndexName = $"idx_auto_{string.Join("_", pattern.FilterFields)}_{DateTime.UtcNow:yyyyMMdd}",
                    Fields = suggestedFields,
                    EstimatedImpact = CalculateIndexImpact(pattern),
                    Reason = $"Pattern executed {pattern.ExecutionCount} times with avg {pattern.AverageExecutionTimeMs:F2}ms"
                });
            }
        }

        return suggestions.OrderByDescending(s => s.EstimatedImpact).ToList();
    }

    /// <summary>
    /// Creates a suggested index.
    /// </summary>
    public async Task CreateSuggestedIndexAsync(
        string collectionName,
        IndexSuggestion suggestion,
        CancellationToken cancellationToken = default)
    {
        var config = new IndexConfig
        {
            Name = suggestion.SuggestedIndexName,
            Fields = suggestion.Fields,
            Type = IndexType.Compound,
            IsAutoGenerated = true
        };

        var collection = _mongoDb.GetCollection<BsonDocument>(collectionName);
        await CreateIndexFromConfigAsync(collection, config, cancellationToken);
    }

    /// <summary>
    /// Calculates the estimated impact of creating an index.
    /// </summary>
    private double CalculateIndexImpact(DetectedQueryPattern pattern)
    {
        // Simple heuristic based on execution frequency and scan ratio
        var frequencyScore = Math.Min(pattern.ExecutionCount / 1000.0, 10.0);
        var scanRatioScore = pattern.AverageDocsExamined > 0
            ? Math.Min(pattern.AverageDocsExamined / Math.Max(pattern.AverageDocsReturned, 1), 10.0)
            : 0;
        var latencyScore = Math.Min(pattern.AverageExecutionTimeMs / 100.0, 10.0);

        return frequencyScore * scanRatioScore * latencyScore;
    }

    private bool IsIndexCreated(string collectionName, string indexName)
    {
        return _createdIndexes.TryGetValue(collectionName, out var indexes) && indexes.Contains(indexName);
    }

    private void MarkIndexCreated(string collectionName, string indexName)
    {
        if (!_createdIndexes.TryGetValue(collectionName, out var indexes))
        {
            indexes = new HashSet<string>();
            _createdIndexes[collectionName] = indexes;
        }
        indexes.Add(indexName);
    }

    /// <summary>
    /// Creates a MongoDB Collation from configuration.
    /// </summary>
    private Collation CreateMongoCollation(CollationConfig config)
    {
        // Parse CaseFirst - use string comparison since the enum might not be available
        var caseFirst = CollationCaseFirst.Off;
        if (!string.IsNullOrEmpty(config.CaseFirst))
        {
            if (Enum.TryParse<CollationCaseFirst>(config.CaseFirst, true, out var parsed))
            {
                caseFirst = parsed;
            }
        }

        // Parse Alternate
        var alternate = CollationAlternate.NonIgnorable;
        if (!string.IsNullOrEmpty(config.Alternate))
        {
            if (Enum.TryParse<CollationAlternate>(config.Alternate, true, out var parsed))
            {
                alternate = parsed;
            }
        }

        // Parse MaxVariable
        var maxVariable = CollationMaxVariable.Punctuation;
        if (!string.IsNullOrEmpty(config.MaxVariable))
        {
            if (Enum.TryParse<CollationMaxVariable>(config.MaxVariable, true, out var parsed))
            {
                maxVariable = parsed;
            }
        }

        return new Collation(
            config.Locale,
            caseLevel: config.CaseLevel,
            caseFirst: caseFirst,
            strength: (CollationStrength)config.Strength,
            numericOrdering: config.NumericOrdering,
            alternate: alternate,
            maxVariable: maxVariable,
            normalization: config.Normalization,
            backwards: config.Backwards
        );
    }
}

/// <summary>
/// Information about a collection index.
/// </summary>
public class IndexInfo
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, int> Keys { get; set; } = new();
    public bool IsUnique { get; set; }
    public bool IsSparse { get; set; }
}

/// <summary>
/// Index usage information.
/// </summary>
public class IndexUsageInfo
{
    public string IndexName { get; set; } = string.Empty;
    public int Accesses { get; set; }
    public DateTime Since { get; set; }
}

/// <summary>
/// Suggested index based on query pattern analysis.
/// </summary>
public class IndexSuggestion
{
    public string PatternHash { get; set; } = string.Empty;
    public string SuggestedIndexName { get; set; } = string.Empty;
    public Dictionary<string, int> Fields { get; set; } = new();
    public double EstimatedImpact { get; set; }
    public string Reason { get; set; } = string.Empty;
}
