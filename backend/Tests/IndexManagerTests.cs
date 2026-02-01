using Xunit;
using Moq;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Valora.Api.Application.Schemas.TemplateConfig;
using Valora.Api.Infrastructure.Projections;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Tests;

/// <summary>
/// Tests for the IndexManager class that handles MongoDB index operations.
/// </summary>
public class IndexManagerTests : IDisposable
{
    private readonly Mock<MongoDbContext> _mongoDbMock;
    private readonly Mock<IMongoDatabase> _databaseMock;
    private readonly Mock<IMongoCollection<BsonDocument>> _collectionMock;
    private readonly Mock<IAsyncCursor<BsonDocument>> _cursorMock;
    private readonly Mock<IMongoIndexManager<BsonDocument>> _indexManagerMock;
    private readonly Mock<ILogger<IndexManager>> _loggerMock;
    private readonly IndexManager _indexManager;

    public IndexManagerTests()
    {
        _mongoDbMock = new Mock<MongoDbContext>(MockBehavior.Loose, (IConfiguration)null!);
        _databaseMock = new Mock<IMongoDatabase>();
        _collectionMock = new Mock<IMongoCollection<BsonDocument>>();
        _cursorMock = new Mock<IAsyncCursor<BsonDocument>>();
        _indexManagerMock = new Mock<IMongoIndexManager<BsonDocument>>();
        _loggerMock = new Mock<ILogger<IndexManager>>();

        _collectionMock.Setup(c => c.Indexes).Returns(_indexManagerMock.Object);
        _collectionMock.Setup(c => c.CollectionNamespace)
            .Returns(new CollectionNamespace(new DatabaseNamespace("test"), "test_collection"));

        _mongoDbMock.Setup(m => m.Database).Returns(_databaseMock.Object);
        _mongoDbMock.Setup(m => m.GetCollection<BsonDocument>(It.IsAny<string>())).Returns(_collectionMock.Object);

        _indexManager = new IndexManager(_mongoDbMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region EnsureIndexesAsync Tests

    [Fact]
    public async Task EnsureIndexesAsync_WithNullConfig_CreatesBaseIndexes()
    {
        // Act
        await _indexManager.EnsureIndexesAsync("TestCollection", null, CancellationToken.None);

        // Assert - Base indexes should be created (tenantId, _projectedAt, compound)
        _indexManagerMock.Verify(
            im => im.CreateOneAsync(
                It.Is<CreateIndexModel<BsonDocument>>(
                    model => model.Options.Name == "idx_tenantId"),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _indexManagerMock.Verify(
            im => im.CreateOneAsync(
                It.Is<CreateIndexModel<BsonDocument>>(
                    model => model.Options.Name == "idx_projectedAt"),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _indexManagerMock.Verify(
            im => im.CreateOneAsync(
                It.Is<CreateIndexModel<BsonDocument>>(
                    model => model.Options.Name == "idx_tenant_projectedAt"),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureIndexesAsync_WithConfig_CreatesConfiguredIndexes()
    {
        // Arrange
        var config = new SmartProjectionConfig
        {
            Indexes = new List<IndexConfig>
            {
                new IndexConfig
                {
                    Name = "idx_custom",
                    Fields = new Dictionary<string, int> { { "CustomField", 1 } },
                    Type = IndexType.Standard,
                    IsUnique = true
                }
            }
        };

        // Act
        await _indexManager.EnsureIndexesAsync("TestCollection", config, CancellationToken.None);

        // Assert
        _indexManagerMock.Verify(
            im => im.CreateOneAsync(
                It.Is<CreateIndexModel<BsonDocument>>(
                    model => model.Options.Name == "idx_custom" && model.Options.Unique == true),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureIndexesAsync_WithTtl_CreatesTtlIndex()
    {
        // Arrange
        var config = new SmartProjectionConfig
        {
            TtlDays = 30
        };

        // Act
        await _indexManager.EnsureIndexesAsync("TestCollection", config, CancellationToken.None);

        // Assert
        _indexManagerMock.Verify(
            im => im.CreateOneAsync(
                It.Is<CreateIndexModel<BsonDocument>>(
                    model => model.Options.Name == "idx_ttl" &&
                             model.Options.ExpireAfter == TimeSpan.FromDays(30)),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureIndexesAsync_WithTextIndex_CreatesTextIndex()
    {
        // Arrange
        var config = new SmartProjectionConfig
        {
            Indexes = new List<IndexConfig>
            {
                new IndexConfig
                {
                    Name = "idx_search",
                    Fields = new Dictionary<string, int> { { "Title", 1 }, { "Description", 1 } },
                    Type = IndexType.Text
                }
            }
        };

        // Act
        await _indexManager.EnsureIndexesAsync("TestCollection", config, CancellationToken.None);

        // Assert
        _indexManagerMock.Verify(
            im => im.CreateOneAsync(
                It.IsAny<CreateIndexModel<BsonDocument>>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task EnsureIndexesAsync_WithCompoundIndex_CreatesCompoundIndex()
    {
        // Arrange
        var config = new SmartProjectionConfig
        {
            Indexes = new List<IndexConfig>
            {
                new IndexConfig
                {
                    Name = "idx_compound",
                    Fields = new Dictionary<string, int>
                    {
                        { "Status", 1 },
                        { "CreatedAt", -1 }
                    },
                    Type = IndexType.Compound
                }
            }
        };

        // Act
        await _indexManager.EnsureIndexesAsync("TestCollection", config, CancellationToken.None);

        // Assert
        _indexManagerMock.Verify(
            im => im.CreateOneAsync(
                It.Is<CreateIndexModel<BsonDocument>>(
                    model => model.Options.Name == "idx_compound"),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureIndexesAsync_WithCollation_CreatesIndexWithCollation()
    {
        // Arrange
        var config = new SmartProjectionConfig
        {
            Indexes = new List<IndexConfig>
            {
                new IndexConfig
                {
                    Name = "idx_name",
                    Fields = new Dictionary<string, int> { { "Name", 1 } },
                    Type = IndexType.Standard,
                    Collation = new CollationConfig
                    {
                        Locale = "en",
                        Strength = 2,
                        CaseFirst = "upper"
                    }
                }
            }
        };

        // Act
        await _indexManager.EnsureIndexesAsync("TestCollection", config, CancellationToken.None);

        // Assert
        _indexManagerMock.Verify(
            im => im.CreateOneAsync(
                It.Is<CreateIndexModel<BsonDocument>>(
                    model => model.Options.Name == "idx_name" &&
                             model.Options.Collation != null &&
                             model.Options.Collation.Locale == "en"),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region DropIndexAsync Tests

    [Fact]
    public async Task DropIndexAsync_CallsDropOneAsync()
    {
        // Arrange
        var indexName = "idx_to_drop";

        // Act
        await _indexManager.DropIndexAsync("TestCollection", indexName, CancellationToken.None);

        // Assert
        _indexManagerMock.Verify(
            im => im.DropOneAsync(indexName, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ListIndexesAsync Tests

    [Fact]
    public async Task ListIndexesAsync_ReturnsIndexList()
    {
        // Arrange
        var indexes = new List<BsonDocument>
        {
            new BsonDocument
            {
                { "name", "_id_" },
                { "key", new BsonDocument { { "_id", 1 } } },
                { "unique", true }
            },
            new BsonDocument
            {
                { "name", "idx_tenantId" },
                { "key", new BsonDocument { { "TenantId", 1 } } },
                { "unique", false }
            }
        };

        _indexManagerMock.Setup(im => im.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockAsyncCursor<BsonDocument>(indexes));

        // Act
        var result = await _indexManager.ListIndexesAsync("TestCollection", CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.Name == "_id_" && i.IsUnique);
        Assert.Contains(result, i => i.Name == "idx_tenantId" && !i.IsUnique);
    }

    #endregion

    #region SuggestIndexesAsync Tests

    [Fact]
    public async Task SuggestIndexesAsync_WithHighFrequencyPatterns_SuggestsIndexes()
    {
        // Arrange
        var patterns = new List<DetectedQueryPattern>
        {
            new DetectedQueryPattern
            {
                PatternHash = "hash1",
                FilterFields = new List<string> { "Status", "CustomerId" },
                SortFields = new List<string> { "CreatedAt" },
                ExecutionCount = 500,
                AverageExecutionTimeMs = 150,
                AverageDocsExamined = 1000,
                AverageDocsReturned = 50,
                IndexCreated = false
            }
        };

        _indexManagerMock.Setup(im => im.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockAsyncCursor<BsonDocument>(new List<BsonDocument>()));

        // Act
        var suggestions = await _indexManager.SuggestIndexesAsync("TestCollection", patterns, CancellationToken.None);

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Fields.ContainsKey("Status"));
        Assert.Contains(suggestions, s => s.Fields.ContainsKey("CustomerId"));
    }

    [Fact]
    public async Task SuggestIndexesAsync_WithExistingCoveringIndex_NoSuggestion()
    {
        // Arrange
        var patterns = new List<DetectedQueryPattern>
        {
            new DetectedQueryPattern
            {
                PatternHash = "hash1",
                FilterFields = new List<string> { "Status" },
                ExecutionCount = 200,
                IndexCreated = false
            }
        };

        var existingIndexes = new List<BsonDocument>
        {
            new BsonDocument
            {
                { "name", "idx_status" },
                { "key", new BsonDocument { { "Status", 1 } } }
            }
        };

        _indexManagerMock.Setup(im => im.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockAsyncCursor<BsonDocument>(existingIndexes));

        // Act
        var suggestions = await _indexManager.SuggestIndexesAsync("TestCollection", patterns, CancellationToken.None);

        // Assert
        Assert.Empty(suggestions);
        Assert.True(patterns[0].IndexCreated); // Should be marked as having index
    }

    #endregion

    #region CreateSuggestedIndexAsync Tests

    [Fact]
    public async Task CreateSuggestedIndexAsync_CreatesIndexFromSuggestion()
    {
        // Arrange
        var suggestion = new IndexSuggestion
        {
            PatternHash = "hash1",
            SuggestedIndexName = "idx_auto_Status_20240101",
            Fields = new Dictionary<string, int> { { "Status", 1 } },
            EstimatedImpact = 75.5,
            Reason = "High frequency pattern"
        };

        // Act
        await _indexManager.CreateSuggestedIndexAsync("TestCollection", suggestion, CancellationToken.None);

        // Assert
        _indexManagerMock.Verify(
            im => im.CreateOneAsync(
                It.Is<CreateIndexModel<BsonDocument>>(
                    model => model.Options.Name == suggestion.SuggestedIndexName &&
                             model.Options.Background == true),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Helper Classes

    private class MockAsyncCursor<T> : IAsyncCursor<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private bool _disposed;

        public MockAsyncCursor(IEnumerable<T> items)
        {
            _enumerator = items.GetEnumerator();
        }

        public IEnumerable<T> Current => new[] { _enumerator.Current };

        public bool MoveNext(CancellationToken cancellationToken = default)
        {
            return _enumerator.MoveNext();
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_enumerator.MoveNext());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _enumerator.Dispose();
                _disposed = true;
            }
        }
    }

    #endregion
}
