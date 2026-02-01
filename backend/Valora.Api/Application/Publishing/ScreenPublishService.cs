using Valora.Api.Application.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using Valora.Api.Domain.Events;
using Valora.Api.Infrastructure.Persistence;
using Valora.Api.Domain.Entities;

namespace Lab360.Application.Publishing
{
    public sealed record ScreenPublishRequest(
        string TenantId,
        string ObjectCode,
        string FromEnv,
        string ToEnv);

    public sealed class ScreenPublishService
    {
        private readonly MongoDbContext _mongoDb;
        private readonly PlatformDbContext _dbContext;

        public ScreenPublishService(MongoDbContext mongoDb, PlatformDbContext dbContext)
        {
            _mongoDb = mongoDb;
            _dbContext = dbContext;
        }

        public async Task PublishAsync(ScreenPublishRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.TenantId))
            {
                throw new ArgumentException("publish.tenantIdRequired");
            }

            if (string.IsNullOrWhiteSpace(request.ObjectCode))
            {
                throw new ArgumentException("publish.objectCodeRequired");
            }

            var tenantId = request.TenantId;
            var objectCode = request.ObjectCode;
            var fromEnv = request.FromEnv.ToLowerInvariant();
            var toEnv = request.ToEnv.ToLowerInvariant();

            var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var filter = Builders<BsonDocument>.Filter.Eq("tenantId", tenantId);
            var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (doc == null)
            {
                throw new ArgumentException("publish.tenantTemplateNotFound");
            }

            if (!doc.TryGetValue("environments", out var environmentsValue) ||
                environmentsValue.IsBsonNull ||
                environmentsValue.BsonType != BsonType.Document)
            {
                throw new ArgumentException("publish.environmentsNotConfigured");
            }

            var environments = environmentsValue.AsBsonDocument;
            if (!environments.TryGetValue(fromEnv, out var fromEnvValue) ||
                fromEnvValue.IsBsonNull ||
                fromEnvValue.BsonType != BsonType.Document)
            {
                throw new ArgumentException("publish.sourceEnvironmentNotFound");
            }

            var fromEnvDoc = fromEnvValue.AsBsonDocument;
            if (!fromEnvDoc.TryGetValue("screens", out var fromScreensValue) ||
                fromScreensValue.IsBsonNull ||
                fromScreensValue.BsonType != BsonType.Document)
            {
                throw new ArgumentException("publish.sourceEnvironmentHasNoScreens");
            }

            var fromScreens = fromScreensValue.AsBsonDocument;
            if (!fromScreens.TryGetValue(objectCode, out var screenValue) || screenValue.IsBsonNull)
            {
                var screenElement = fromScreens.Elements.FirstOrDefault(e =>
                    string.Equals(e.Name, objectCode, StringComparison.OrdinalIgnoreCase));
                if (screenElement.Name == null)
                {
                    throw new ArgumentException("publish.screenNotFoundInSourceEnvironment");
                }

                screenValue = screenElement.Value;
            }

            if (screenValue.IsBsonNull || screenValue.BsonType != BsonType.Document)
            {
                throw new ArgumentException("publish.screenNotFoundInSourceEnvironment");
            }

            var versionsDoc = screenValue.AsBsonDocument;
            var versionEntry = versionsDoc.Elements
                .Where(e => e.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                .Select(e =>
                {
                    var name = e.Name;
                    var numberPart = name.Length > 1 ? name.Substring(1) : "0";
                    if (!int.TryParse(numberPart, out var v))
                    {
                        v = 0;
                    }

                    return new
                    {
                        Version = v,
                        Document = e.Value.AsBsonDocument
                    };
                })
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            if (versionEntry == null || versionEntry.Version == 0)
            {
                throw new ArgumentException("publish.noValidVersionForScreen");
            }

            var versionDoc = versionEntry.Document.DeepClone().AsBsonDocument; // Clone to avoid modifying the original doc reference

            if (!versionDoc.TryGetValue("fields", out var fieldsValue) ||
                fieldsValue.IsBsonNull ||
                fieldsValue.BsonType != BsonType.Document)
            {
                throw new ArgumentException("publish.screenHasNoFields");
            }

            var fields = fieldsValue.AsBsonDocument;
            if (!fields.Elements.Any())
            {
                throw new ArgumentException("publish.screenHasNoFields");
            }

            var nextVersion = 1;

            if (environments.TryGetValue(toEnv, out var toEnvValue) &&
                !toEnvValue.IsBsonNull &&
                toEnvValue.BsonType == BsonType.Document)
            {
                var toEnvDoc = toEnvValue.AsBsonDocument;
                if (toEnvDoc.TryGetValue("screens", out var toScreensValue) &&
                    !toScreensValue.IsBsonNull &&
                    toScreensValue.BsonType == BsonType.Document)
                {
                    var toScreens = toScreensValue.AsBsonDocument;
                    if (toScreens.TryGetValue(objectCode, out var toScreenValue) &&
                        !toScreenValue.IsBsonNull &&
                        toScreenValue.BsonType == BsonType.Document)
                    {
                        var toVersionsDoc = toScreenValue.AsBsonDocument;
                        var maxVersion = toVersionsDoc.Elements
                            .Where(e => e.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                            .Select(e =>
                            {
                                var name = e.Name;
                                var numberPart = name.Length > 1 ? name.Substring(1) : "0";
                                if (!int.TryParse(numberPart, out var v))
                                {
                                    v = 0;
                                }
                                return v;
                            })
                            .DefaultIfEmpty(0)
                            .Max();

                        nextVersion = maxVersion + 1;
                    }
                }
            }

            var toEnvPath = $"environments.{toEnv}.screens.{objectCode}.v{nextVersion}";

            // Ensure isPublished is part of the document being set, rather than a separate Set operation
            // This prevents "Updating the path ... would create a conflict" error
            if (versionDoc.Contains("isPublished"))
            {
                versionDoc["isPublished"] = true;
            }
            else
            {
                versionDoc.Add("isPublished", true);
            }

            var update = Builders<BsonDocument>.Update
                .Set(toEnvPath, versionDoc);

            await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            // Trigger Schema Sync (DDL)
            // Parse schema from versionDoc to ModuleSchema
            var schemaBody = new BsonDocument
            {
                { "fields", fields }
            };

            if (versionDoc.TryGetValue("uniqueConstraints", out var uniqueConstraintsValue) && !uniqueConstraintsValue.IsBsonNull)
            {
                schemaBody.Add("uniqueConstraints", uniqueConstraintsValue);
            }

            if (versionDoc.TryGetValue("ui", out var uiValue) && !uiValue.IsBsonNull)
            {
                schemaBody.Add("ui", uiValue);
            }

            if (versionDoc.TryGetValue("objectType", out var objectTypeValue) && !objectTypeValue.IsBsonNull)
            {
                schemaBody.Add("objectType", objectTypeValue);
            }

            var jsonSettings = new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson };
            var schemaJson = schemaBody.ToJson(jsonSettings);
            
            // Create Outbox Message for Schema Sync
            var outboxMessage = new OutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Topic = "valora.schema.changed",
                Payload = JsonSerializer.Serialize(new SchemaChangedEvent
                {
                    TenantId = tenantId,
                    ModuleCode = objectCode,
                    AggregateType = "Schema", // Traceability
                    AggregateId = $"{objectCode}_v{nextVersion}", // Traceability
                    Version = nextVersion, // Use the new version number
                    SchemaJson = schemaJson
                }),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.OutboxMessages.Add(outboxMessage);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
