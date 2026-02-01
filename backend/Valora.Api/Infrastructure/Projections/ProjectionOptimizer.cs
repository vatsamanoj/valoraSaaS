using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Application.Schemas.TemplateConfig;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Infrastructure.Projections;

/// <summary>
/// Analyzes query patterns and automatically optimizes projections.
/// Detects frequently used query patterns, suggests indexes, and manages denormalization.
/// </summary>
public class ProjectionOptimizer
{
    private readonly MongoDbContext _mongoDb;
    private readonly IndexManager _indexManager;
    private readonly ILogger<ProjectionOptimizer> _logger;

    // Query pattern tracking
    private readonly ConcurrentDictionary<string, DetectedQueryPattern> _queryPatterns = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastAnalysis = new();

    // Configuration cache
    private readonly ConcurrentDictionary<string, SmartProjectionConfig> _configCache = new();

    public ProjectionOptimizer(
        MongoDbContext mongoDb,
        IndexManager indexManager,
        ILogger<ProjectionOptimizer> logger)
    {
        _mongoDb = mongoDb;
        _indexManager = indexManager;
        _logger = logger;
    }

    /// <summary>
    /// Records a query execution for pattern analysis.
    /// </summary>
    public void RecordQuery(
        string collectionName,
        FilterDefinition<BsonDocument> filter,
        SortDefinition<BsonDocument>? sort = null,
        ProjectionDefinition<BsonDocument>? projection = null,
        long executionTimeMs = 0,
        long docsExamined = 0,
        long docsReturned = 0)
    {
        try
        {
            var pattern = ExtractQueryPattern(filter, sort, projection);
            var patternHash = ComputePatternHash(pattern);
            var key = $"{collectionName}:{patternHash}";

            _queryPatterns.AddOrUpdate(key, _ => new DetectedQueryPattern
            {
                PatternHash = patternHash,
                FilterFields = pattern.FilterFields,
                SortFields = pattern.SortFields,
                ProjectionFields = pattern.ProjectionFields,
                ExecutionCount = 1,
                AverageExecutionTimeMs = executionTimeMs,
                AverageDocsExamined = docsExamined,
                AverageDocsReturned = docsReturned,
                FirstDetectedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            }, (_, existing) =>
            {
                existing.ExecutionCount++;
                existing.AverageExecutionTimeMs =
                    (existing.AverageExecutionTimeMs * (existing.ExecutionCount - 1) + executionTimeMs) /
                    existing.ExecutionCount;
                existing.AverageDocsExamined =
                    (existing.AverageDocsExamined * (existing.ExecutionCount - 1) + docsExamined) /
                    existing.ExecutionCount;
                existing.AverageDocsReturned =
                    (existing.AverageDocsReturned * (existing.ExecutionCount - 1) + docsReturned) /
                    existing.ExecutionCount;
                existing.LastSeenAt = DateTime.UtcNow;
                return existing;
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to record query pattern for {CollectionName}", collectionName);
        }
    }

    /// <summary>
    /// Analyzes query patterns for a collection and applies optimizations.
    /// </summary>
    public async Task<OptimizationResult> AnalyzeAndOptimizeAsync(
        string collectionName,
        SmartProjectionConfig config,
        CancellationToken cancellationToken = default)
    {
        var result = new OptimizationResult { CollectionName = collectionName };

        if (!config.AutoOptimize)
        {
            _logger.LogDebug("Auto-optimization disabled for {CollectionName}", collectionName);
            return result;
        }

        // Check if we should run analysis based on configured window
        var lastAnalysis = _lastAnalysis.GetValueOrDefault(collectionName, DateTime.MinValue);
        var windowHours = config.QueryPatternTracking?.AnalysisWindowHours ?? 24;
        if (DateTime.UtcNow - lastAnalysis < TimeSpan.FromHours(windowHours))
        {
            _logger.LogDebug("Skipping analysis for {CollectionName}, last analysis was at {LastAnalysis}",
                collectionName, lastAnalysis);
            return result;
        }

        try
        {
            // Get patterns for this collection
            var patterns = _queryPatterns
                .Where(kvp => kvp.Key.StartsWith($"{collectionName}:"))
                .Select(kvp => kvp.Value)
                .ToList();

            if (!patterns.Any())
            {
                _logger.LogDebug("No query patterns recorded for {CollectionName}", collectionName);
                return result;
            }

            // Analyze for index suggestions
            if (config.QueryPatternTracking?.AutoCreateIndexes != false)
            {
                var suggestions = await _indexManager.SuggestIndexesAsync(collectionName, patterns, cancellationToken);
                result.SuggestedIndexes = suggestions;

                // Auto-create high-impact indexes
                foreach (var suggestion in suggestions.Where(s => s.EstimatedImpact > 50))
                {
                    try
                    {
                        await _indexManager.CreateSuggestedIndexAsync(collectionName, suggestion, cancellationToken);
                        result.CreatedIndexes.Add(suggestion.SuggestedIndexName);
                        _logger.LogInformation("Auto-created index {IndexName} for {CollectionName}",
                            suggestion.SuggestedIndexName, collectionName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to auto-create index {IndexName}", suggestion.SuggestedIndexName);
                    }
                }
            }

            // Analyze for denormalization suggestions
            if (config.QueryPatternTracking?.AutoSuggestDenormalizations != false)
            {
                var denormSuggestions = AnalyzeDenormalizationPatterns(patterns, config);
                result.SuggestedDenormalizations = denormSuggestions;
            }

            // Clean up old patterns
            CleanupOldPatterns(collectionName, windowHours);

            _lastAnalysis[collectionName] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during optimization analysis for {CollectionName}", collectionName);
        }

        return result;
    }

    /// <summary>
    /// Applies denormalization strategies based on configuration.
    /// </summary>
    public async Task ApplyDenormalizationAsync(
        string collectionName,
        string aggregateType,
        BsonDocument document,
        List<DenormalizationConfig> configs,
        CancellationToken cancellationToken = default)
    {
        foreach (var config in configs.Where(c => c.UpdateStrategy == DenormalizationUpdateStrategy.OnWrite))
        {
            try
            {
                await ApplyDenormalizationConfigAsync(collectionName, aggregateType, document, config, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply denormalization {DenormalizationName} for {CollectionName}",
                    config.Name, collectionName);
            }
        }
    }

    /// <summary>
    /// Compresses large fields in a document based on compression configuration.
    /// </summary>
    public BsonDocument CompressLargeFields(BsonDocument document, CompressionConfig? config)
    {
        if (config?.Enabled != true || !config.Fields.Any())
        {
            return document;
        }

        var result = document.Clone().AsBsonDocument;

        foreach (var fieldPattern in config.Fields)
        {
            var matchingFields = FindMatchingFields(result, fieldPattern);
            foreach (var fieldPath in matchingFields)
            {
                try
                {
                    CompressField(result, fieldPath, config);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to compress field {FieldPath}", fieldPath);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets query pattern statistics for a collection.
    /// </summary>
    public QueryPatternStats GetPatternStats(string collectionName)
    {
        var patterns = _queryPatterns
            .Where(kvp => kvp.Key.StartsWith($"{collectionName}:"))
            .Select(kvp => kvp.Value)
            .ToList();

        return new QueryPatternStats
        {
            TotalPatterns = patterns.Count,
            TotalExecutions = patterns.Sum(p => p.ExecutionCount),
            AverageExecutionTimeMs = patterns.Any() ? patterns.Average(p => p.AverageExecutionTimeMs) : 0,
            TopPatterns = patterns
                .OrderByDescending(p => p.ExecutionCount)
                .Take(10)
                .ToList(),
            PatternsNeedingIndexes = patterns
                .Where(p => !p.IndexCreated && p.ExecutionCount > 100)
                .OrderByDescending(p => p.ExecutionCount)
                .ToList()
        };
    }

    /// <summary>
    /// Clears all recorded query patterns (useful for testing or resetting).
    /// </summary>
    public void ClearPatterns()
    {
        _queryPatterns.Clear();
        _lastAnalysis.Clear();
        _logger.LogInformation("Cleared all query patterns");
    }

    #region Private Helper Methods

    private QueryPattern ExtractQueryPattern(
        FilterDefinition<BsonDocument> filter,
        SortDefinition<BsonDocument>? sort,
        ProjectionDefinition<BsonDocument>? projection)
    {
        var pattern = new QueryPattern();

        // Extract filter fields
        var filterDoc = filter.ToBsonDocument();
        pattern.FilterFields = ExtractFieldNames(filterDoc).ToList();

        // Extract sort fields
        if (sort != null)
        {
            var sortDoc = sort.ToBsonDocument();
            pattern.SortFields = sortDoc.Select(e => e.Name).ToList();
        }

        // Extract projection fields
        if (projection != null)
        {
            var projectionDoc = projection.ToBsonDocument();
            pattern.ProjectionFields = projectionDoc.Select(e => e.Name).ToList();
        }

        return pattern;
    }

    private IEnumerable<string> ExtractFieldNames(BsonDocument doc, string prefix = "")
    {
        foreach (var element in doc)
        {
            var fieldName = string.IsNullOrEmpty(prefix) ? element.Name : $"{prefix}.{element.Name}";

            if (element.Name.StartsWith("$"))
            {
                // Operator - look inside
                if (element.Value.IsBsonDocument)
                {
                    foreach (var nested in ExtractFieldNames(element.Value.AsBsonDocument, prefix))
                    {
                        yield return nested;
                    }
                }
                else if (element.Value.IsBsonArray)
                {
                    foreach (var item in element.Value.AsBsonArray.Where(i => i.IsBsonDocument))
                    {
                        foreach (var nested in ExtractFieldNames(item.AsBsonDocument, prefix))
                        {
                            yield return nested;
                        }
                    }
                }
            }
            else
            {
                yield return fieldName;

                // Recurse into nested documents
                if (element.Value.IsBsonDocument)
                {
                    foreach (var nested in ExtractFieldNames(element.Value.AsBsonDocument, fieldName))
                    {
                        yield return nested;
                    }
                }
            }
        }
    }

    private string ComputePatternHash(QueryPattern pattern)
    {
        var hashInput = JsonSerializer.Serialize(new
        {
            Filters = string.Join(",", pattern.FilterFields.OrderBy(f => f)),
            Sorts = string.Join(",", pattern.SortFields.OrderBy(f => f)),
            Projections = pattern.ProjectionFields != null
                ? string.Join(",", pattern.ProjectionFields.OrderBy(f => f))
                : ""
        });

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToHexString(hash)[..16];
    }

    private List<DenormalizationSuggestion> AnalyzeDenormalizationPatterns(
        List<DetectedQueryPattern> patterns,
        SmartProjectionConfig config)
    {
        var suggestions = new List<DenormalizationSuggestion>();

        // Look for patterns that include lookups/joins (indicated by foreign key fields)
        var foreignKeyPatterns = patterns
            .Where(p => p.FilterFields.Any(f => f.EndsWith("Id") && f != "_id"))
            .GroupBy(p => string.Join(",", p.FilterFields.Where(f => f.EndsWith("Id") && f != "_id").OrderBy(f => f)))
            .OrderByDescending(g => g.Sum(p => p.ExecutionCount))
            .Take(5);

        foreach (var group in foreignKeyPatterns)
        {
            var totalExecutions = group.Sum(p => p.ExecutionCount);
            var avgExecutionTime = group.Average(p => p.AverageExecutionTimeMs);

            if (totalExecutions > 50 && avgExecutionTime > 50)
            {
                suggestions.Add(new DenormalizationSuggestion
                {
                    ForeignKeyFields = group.Key.Split(",").ToList(),
                    ExecutionCount = totalExecutions,
                    AverageExecutionTimeMs = avgExecutionTime,
                    SuggestedStrategy = "Embed related documents to reduce lookup overhead",
                    EstimatedBenefit = $"Reduce query time by ~{Math.Min(80, totalExecutions / 10):F0}%"
                });
            }
        }

        return suggestions;
    }

    private async Task ApplyDenormalizationConfigAsync(
        string collectionName,
        string aggregateType,
        BsonDocument document,
        DenormalizationConfig config,
        CancellationToken cancellationToken)
    {
        if (!document.Contains(config.ForeignKeyField))
        {
            return;
        }

        var foreignKey = document[config.ForeignKeyField];
        if (foreignKey == BsonNull.Value || foreignKey.BsonType == BsonType.Null)
        {
            return;
        }

        // Fetch related data from source collection
        var sourceCollection = _mongoDb.GetCollection<BsonDocument>($"Entity_{config.SourceEntity}");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", foreignKey);
        var projection = BuildProjection(config.SourceFields);

        var relatedDoc = await sourceCollection
            .Find(filter)
            .Project(projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (relatedDoc != null)
        {
            // Remove _id from embedded document
            relatedDoc.Remove("_id");

            // Set the denormalized data
            SetNestedValue(document, config.TargetFieldPath, relatedDoc);

            _logger.LogDebug("Applied denormalization {ConfigName} to document in {CollectionName}",
                config.Name, collectionName);
        }
    }

    private ProjectionDefinition<BsonDocument> BuildProjection(List<string> fields)
    {
        var projection = Builders<BsonDocument>.Projection.Include("_id");
        foreach (var field in fields)
        {
            projection = projection.Include(field);
        }
        return projection;
    }

    private void SetNestedValue(BsonDocument document, string path, BsonValue value)
    {
        var parts = path.Split('.');
        var current = document;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!current.Contains(parts[i]) || !current[parts[i]].IsBsonDocument)
            {
                current[parts[i]] = new BsonDocument();
            }
            current = current[parts[i]].AsBsonDocument;
        }

        current[parts[^1]] = value;
    }

    private List<string> FindMatchingFields(BsonDocument document, string pattern)
    {
        var matches = new List<string>();
        var isWildcard = pattern.Contains("*");

        void Search(BsonValue value, string currentPath)
        {
            if (!value.IsBsonDocument) return;

            var doc = value.AsBsonDocument;
            foreach (var element in doc)
            {
                var fieldPath = string.IsNullOrEmpty(currentPath)
                    ? element.Name
                    : $"{currentPath}.{element.Name}";

                if (isWildcard)
                {
                    // Simple wildcard matching
                    var regex = pattern.Replace(".", "\\.").Replace("*", ".*");
                    if (System.Text.RegularExpressions.Regex.IsMatch(fieldPath, $"^{regex}$"))
                    {
                        matches.Add(fieldPath);
                    }
                }
                else if (element.Name == pattern || fieldPath == pattern)
                {
                    matches.Add(fieldPath);
                }

                // Recurse
                if (element.Value.IsBsonDocument)
                {
                    Search(element.Value, fieldPath);
                }
                else if (element.Value.IsBsonArray)
                {
                    foreach (var item in element.Value.AsBsonArray)
                    {
                        Search(item, fieldPath);
                    }
                }
            }
        }

        Search(document, "");
        return matches;
    }

    private void CompressField(BsonDocument document, string fieldPath, CompressionConfig config)
    {
        // This is a placeholder for actual compression logic
        // In a real implementation, you would:
        // 1. Get the field value
        // 2. Check if it exceeds MinSizeBytes
        // 3. Compress using the specified algorithm
        // 4. Store with a marker indicating it's compressed

        _logger.LogDebug("Field compression for {FieldPath} would be applied here", fieldPath);
    }

    private void CleanupOldPatterns(string collectionName, int windowHours)
    {
        var cutoff = DateTime.UtcNow.AddHours(-windowHours);
        var keysToRemove = _queryPatterns
            .Where(kvp => kvp.Key.StartsWith($"{collectionName}:") && kvp.Value.LastSeenAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _queryPatterns.TryRemove(key, out _);
        }

        if (keysToRemove.Any())
        {
            _logger.LogDebug("Cleaned up {Count} old query patterns for {CollectionName}",
                keysToRemove.Count, collectionName);
        }
    }

    #endregion
}

/// <summary>
/// Internal class for query pattern extraction.
/// </summary>
internal class QueryPattern
{
    public List<string> FilterFields { get; set; } = new();
    public List<string> SortFields { get; set; } = new();
    public List<string>? ProjectionFields { get; set; }
}

/// <summary>
/// Result of an optimization analysis.
/// </summary>
public class OptimizationResult
{
    public string CollectionName { get; set; } = string.Empty;
    public List<IndexSuggestion> SuggestedIndexes { get; set; } = new();
    public List<string> CreatedIndexes { get; set; } = new();
    public List<DenormalizationSuggestion> SuggestedDenormalizations { get; set; } = new();
    public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Suggestion for denormalization.
/// </summary>
public class DenormalizationSuggestion
{
    public List<string> ForeignKeyFields { get; set; } = new();
    public long ExecutionCount { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public string SuggestedStrategy { get; set; } = string.Empty;
    public string EstimatedBenefit { get; set; } = string.Empty;
}

/// <summary>
/// Statistics about query patterns.
/// </summary>
public class QueryPatternStats
{
    public int TotalPatterns { get; set; }
    public long TotalExecutions { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public List<DetectedQueryPattern> TopPatterns { get; set; } = new();
    public List<DetectedQueryPattern> PatternsNeedingIndexes { get; set; } = new();
}
