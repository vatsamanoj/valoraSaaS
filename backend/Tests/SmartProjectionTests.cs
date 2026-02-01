using Xunit;
using Moq;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Valora.Api.Application.Schemas;
using Valora.Api.Application.Schemas.TemplateConfig;
using Valora.Api.Infrastructure.Projections;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Tests;

/// <summary>
/// Tests for the Smart Projection system including SmartProjectionService,
/// IndexManager, and ProjectionOptimizer integration.
/// </summary>
public class SmartProjectionTests : IDisposable
{
    private readonly Mock<MongoDbContext> _mongoDbMock;
    private readonly Mock<IMongoDatabase> _databaseMock;
    private readonly Mock<IMongoCollection<BsonDocument>> _collectionMock;
    private readonly Mock<IAsyncCursor<BsonDocument>> _cursorMock;
    private readonly Mock<IndexManager> _indexManagerMock;
    private readonly Mock<ProjectionOptimizer> _optimizerMock;
    private readonly Mock<ISchemaProvider> _schemaProviderMock;
    private readonly Mock<ILogger<SmartProjectionService>> _serviceLoggerMock;
    private readonly SmartProjectionService _service;

    public SmartProjectionTests()
    {
        _mongoDbMock = new Mock<MongoDbContext>(MockBehavior.Loose, (IConfiguration)null!);
        _databaseMock = new Mock<IMongoDatabase>();
        _collectionMock = new Mock<IMongoCollection<BsonDocument>>();
        _cursorMock = new Mock<IAsyncCursor<BsonDocument>>();

        _mongoDbMock.Setup(m => m.Database).Returns(_databaseMock.Object);
        _mongoDbMock.Setup(m => m.GetCollection<BsonDocument>(It.IsAny<string>())).Returns(_collectionMock.Object);

        _indexManagerMock = new Mock<IndexManager>(
            _mongoDbMock.Object,
            Mock.Of<ILogger<IndexManager>>());

        _optimizerMock = new Mock<ProjectionOptimizer>(
            _mongoDbMock.Object,
            _indexManagerMock.Object,
            Mock.Of<ILogger<ProjectionOptimizer>>());

        _schemaProviderMock = new Mock<ISchemaProvider>();
        _serviceLoggerMock = new Mock<ILogger<SmartProjectionService>>();

        _service = new SmartProjectionService(
            _mongoDbMock.Object,
            _indexManagerMock.Object,
            _optimizerMock.Object,
            _schemaProviderMock.Object,
            _serviceLoggerMock.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region SmartProjectionConfig Tests

    [Fact]
    public void SmartProjectionConfig_DefaultValues_AreCorrect()
    {
        var config = new SmartProjectionConfig();

        Assert.True(config.AutoOptimize);
        Assert.Empty(config.Indexes);
        Assert.Empty(config.Denormalizations);
        Assert.Null(config.TtlDays);
        Assert.Null(config.Sharding);
        Assert.Null(config.Caching);
        Assert.Empty(config.AggregationPipelines);
        Assert.Null(config.Validation);
        Assert.Null(config.Archival);
        Assert.Null(config.Compression);
        Assert.Null(config.QueryPatternTracking);
    }

    [Fact]
    public void SmartProjectionConfig_CanBeConfigured()
    {
        var config = new SmartProjectionConfig
        {
            AutoOptimize = false,
            TtlDays = 90,
            Indexes = new List<IndexConfig>
            {
                new IndexConfig
                {
                    Name = "idx_test",
                    Fields = new Dictionary<string, int> { { "Field1", 1 } },
                    Type = IndexType.Standard,
                    IsUnique = true
                }
            },
            QueryPatternTracking = new QueryPatternConfig
            {
                Enabled = true,
                AutoCreateIndexes = true
            }
        };

        Assert.False(config.AutoOptimize);
        Assert.Equal(90, config.TtlDays);
        Assert.Single(config.Indexes);
        Assert.NotNull(config.QueryPatternTracking);
        Assert.True(config.QueryPatternTracking.AutoCreateIndexes);
    }

    #endregion

    #region IndexConfig Tests

    [Theory]
    [InlineData(IndexType.Standard)]
    [InlineData(IndexType.Text)]
    [InlineData(IndexType.Hashed)]
    [InlineData(IndexType.Wildcard)]
    [InlineData(IndexType.Compound)]
    public void IndexConfig_AllTypes_AreValid(IndexType type)
    {
        var config = new IndexConfig
        {
            Name = "idx_test",
            Type = type,
            Fields = new Dictionary<string, int> { { "field", 1 } }
        };

        Assert.Equal(type, config.Type);
    }

    [Fact]
    public void IndexConfig_DefaultValues_AreCorrect()
    {
        var config = new IndexConfig();

        Assert.Empty(config.Name);
        Assert.Empty(config.Fields);
        Assert.Equal(IndexType.Standard, config.Type);
        Assert.False(config.IsUnique);
        Assert.False(config.IsSparse);
        Assert.Null(config.PartialFilterExpression);
        Assert.Null(config.Collation);
        Assert.Null(config.ExpireAfterSeconds);
        Assert.False(config.IsAutoGenerated);
        Assert.Null(config.UsageStats);
    }

    [Fact]
    public void IndexConfig_CanSetCollation()
    {
        var config = new IndexConfig
        {
            Collation = new CollationConfig
            {
                Locale = "en",
                Strength = 2,
                CaseFirst = "upper"
            }
        };

        Assert.NotNull(config.Collation);
        Assert.Equal("en", config.Collation.Locale);
        Assert.Equal(2, config.Collation.Strength);
    }

    #endregion

    #region DenormalizationConfig Tests

    [Theory]
    [InlineData(DenormalizationUpdateStrategy.OnWrite)]
    [InlineData(DenormalizationUpdateStrategy.OnRead)]
    [InlineData(DenormalizationUpdateStrategy.Scheduled)]
    [InlineData(DenormalizationUpdateStrategy.EventDriven)]
    public void DenormalizationConfig_AllStrategies_AreValid(DenormalizationUpdateStrategy strategy)
    {
        var config = new DenormalizationConfig
        {
            Name = "test",
            UpdateStrategy = strategy
        };

        Assert.Equal(strategy, config.UpdateStrategy);
    }

    [Fact]
    public void DenormalizationConfig_CanBeConfigured()
    {
        var config = new DenormalizationConfig
        {
            Name = "CustomerLookup",
            SourceEntity = "Customer",
            TargetFieldPath = "customerInfo",
            SourceFields = new List<string> { "Name", "Email", "Phone" },
            ForeignKeyField = "CustomerId",
            UpdateStrategy = DenormalizationUpdateStrategy.OnWrite,
            EmbedAsArray = false,
            MaxDepth = 2
        };

        Assert.Equal("CustomerLookup", config.Name);
        Assert.Equal(3, config.SourceFields.Count);
        Assert.Equal(2, config.MaxDepth);
    }

    #endregion

    #region CacheConfig Tests

    [Theory]
    [InlineData(CacheProvider.Memory)]
    [InlineData(CacheProvider.Redis)]
    [InlineData(CacheProvider.Distributed)]
    public void CacheConfig_AllProviders_AreValid(CacheProvider provider)
    {
        var config = new CacheConfig
        {
            Provider = provider
        };

        Assert.Equal(provider, config.Provider);
    }

    [Theory]
    [InlineData(EvictionPolicy.LRU)]
    [InlineData(EvictionPolicy.LFU)]
    [InlineData(EvictionPolicy.Random)]
    public void CacheConfig_AllEvictionPolicies_AreValid(EvictionPolicy policy)
    {
        var config = new CacheConfig
        {
            EvictionPolicy = policy
        };

        Assert.Equal(policy, config.EvictionPolicy);
    }

    #endregion

    #region SmartProjectionService Tests

    [Fact]
    public void GetDefaultConfig_ReturnsValidConfig()
    {
        var config = _service.GetDefaultConfig("Transaction");

        Assert.NotNull(config);
        Assert.True(config.AutoOptimize);
        Assert.NotNull(config.QueryPatternTracking);
        Assert.True(config.QueryPatternTracking.Enabled);
    }

    [Fact]
    public void GetDefaultConfig_ForTransaction_HasStatusDateIndex()
    {
        var config = _service.GetDefaultConfig("Transaction");

        var statusDateIndex = config.Indexes.FirstOrDefault(i => i.Name == "idx_status_date");
        Assert.NotNull(statusDateIndex);
        Assert.Equal(IndexType.Compound, statusDateIndex.Type);
        Assert.True(statusDateIndex.Fields.ContainsKey("Status"));
        Assert.True(statusDateIndex.Fields.ContainsKey("DocumentDate"));
    }

    [Fact]
    public void GetDefaultConfig_ForMaster_HasCodeIndex()
    {
        var config = _service.GetDefaultConfig("Master");

        var codeIndex = config.Indexes.FirstOrDefault(i => i.Name == "idx_code");
        Assert.NotNull(codeIndex);
        Assert.True(codeIndex.IsUnique);
    }

    [Fact]
    public void GetDefaultConfig_Always_HasIsActiveIndex()
    {
        var transactionConfig = _service.GetDefaultConfig("Transaction");
        var masterConfig = _service.GetDefaultConfig("Master");

        Assert.Contains(transactionConfig.Indexes, i => i.Name == "idx_is_active");
        Assert.Contains(masterConfig.Indexes, i => i.Name == "idx_is_active");
    }

    #endregion

    #region QueryPatternConfig Tests

    [Fact]
    public void QueryPatternConfig_DefaultValues_AreCorrect()
    {
        var config = new QueryPatternConfig();

        Assert.True(config.Enabled);
        Assert.Equal(0.1, config.SampleRate);
        Assert.Equal(1000, config.MaxPatterns);
        Assert.Equal(100, config.MinQueryCountForAutoIndex);
        Assert.Equal(24, config.AnalysisWindowHours);
        Assert.True(config.AutoCreateIndexes);
        Assert.True(config.AutoSuggestDenormalizations);
    }

    #endregion

    #region DocumentValidationConfig Tests

    [Theory]
    [InlineData(ValidationLevel.Off)]
    [InlineData(ValidationLevel.Strict)]
    [InlineData(ValidationLevel.Moderate)]
    public void DocumentValidationConfig_AllLevels_AreValid(ValidationLevel level)
    {
        var config = new DocumentValidationConfig
        {
            Level = level
        };

        Assert.Equal(level, config.Level);
    }

    [Theory]
    [InlineData(ValidationAction.Error)]
    [InlineData(ValidationAction.Warn)]
    public void DocumentValidationConfig_AllActions_AreValid(ValidationAction action)
    {
        var config = new DocumentValidationConfig
        {
            Action = action
        };

        Assert.Equal(action, config.Action);
    }

    #endregion

    #region ArchivalConfig Tests

    [Theory]
    [InlineData(ArchiveDestination.ColdStorage)]
    [InlineData(ArchiveDestination.S3)]
    [InlineData(ArchiveDestination.SeparateCollection)]
    public void ArchivalConfig_AllDestinations_AreValid(ArchiveDestination destination)
    {
        var config = new ArchivalConfig
        {
            Destination = destination
        };

        Assert.Equal(destination, config.Destination);
    }

    [Theory]
    [InlineData(ArchiveCompression.None)]
    [InlineData(ArchiveCompression.Gzip)]
    [InlineData(ArchiveCompression.Bzip2)]
    [InlineData(ArchiveCompression.Lz4)]
    public void ArchivalConfig_AllCompressions_AreValid(ArchiveCompression compression)
    {
        var config = new ArchivalConfig
        {
            Compression = compression
        };

        Assert.Equal(compression, config.Compression);
    }

    [Fact]
    public void ArchivalConfig_DefaultSchedule_IsDailyAtMidnight()
    {
        var config = new ArchivalConfig();

        Assert.Equal("0 0 * * *", config.Schedule);
    }

    #endregion

    #region CompressionConfig Tests

    [Theory]
    [InlineData(CompressionAlgorithm.Gzip)]
    [InlineData(CompressionAlgorithm.Zstd)]
    [InlineData(CompressionAlgorithm.Lz4)]
    [InlineData(CompressionAlgorithm.Snappy)]
    public void CompressionConfig_AllAlgorithms_AreValid(CompressionAlgorithm algorithm)
    {
        var config = new CompressionConfig
        {
            Algorithm = algorithm
        };

        Assert.Equal(algorithm, config.Algorithm);
    }

    [Fact]
    public void CompressionConfig_DefaultValues_AreCorrect()
    {
        var config = new CompressionConfig();

        Assert.False(config.Enabled);
        Assert.Empty(config.Fields);
        Assert.Equal(1024, config.MinSizeBytes);
        Assert.Equal(CompressionAlgorithm.Zstd, config.Algorithm);
        Assert.Equal(3, config.Level);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SmartProjectionConfig_CanSerializeToJson()
    {
        var config = new SmartProjectionConfig
        {
            AutoOptimize = true,
            TtlDays = 30,
            Indexes = new List<IndexConfig>
            {
                new IndexConfig
                {
                    Name = "idx_test",
                    Fields = new Dictionary<string, int> { { "Field1", 1 }, { "Field2", -1 } },
                    Type = IndexType.Compound
                }
            },
            QueryPatternTracking = new QueryPatternConfig
            {
                Enabled = true,
                SampleRate = 0.5
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(config);

        Assert.NotNull(json);
        Assert.Contains("idx_test", json);
        Assert.Contains("Field1", json);
        Assert.Contains("30", json);
    }

    [Fact]
    public void SmartProjectionConfig_CanDeserializeFromJson()
    {
        var json = @"{
            ""autoOptimize"": true,
            ""ttlDays"": 60,
            ""indexes"": [
                {
                    ""name"": ""idx_createdAt"",
                    ""fields"": { ""CreatedAt"": -1 },
                    ""type"": ""Standard""
                }
            ]
        }";

        var config = System.Text.Json.JsonSerializer.Deserialize<SmartProjectionConfig>(json);

        Assert.NotNull(config);
        Assert.True(config.AutoOptimize);
        Assert.Equal(60, config.TtlDays);
        Assert.Single(config.Indexes);
        Assert.Equal("idx_createdAt", config.Indexes[0].Name);
    }

    #endregion
}
