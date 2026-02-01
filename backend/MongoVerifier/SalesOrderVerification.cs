using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace MongoVerifier
{
    /// <summary>
    /// MongoDB Verification Tests for Sales Order Template Extension
    /// Verifies all 7 versions exist with correct schema structure
    /// </summary>
    public class SalesOrderVerification
    {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly string _tenantId;
        private readonly TextWriter _output;

        public SalesOrderVerification(string connectionString, string databaseName, string tenantId, TextWriter output)
        {
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(databaseName);
            _tenantId = tenantId;
            _output = output;
        }

        /// <summary>
        /// Run all verification tests
        /// </summary>
        public async Task<bool> RunAllVerificationsAsync()
        {
            _output.WriteLine("=== Sales Order MongoDB Verification ===\n");

            var results = new List<bool>
            {
                await VerifyAllVersionsExistAsync(),
                await VerifySchemaStructureForAllVersionsAsync(),
                await VerifyCalculationRulesConfigAsync(),
                await VerifyDocumentTotalsConfigAsync(),
                await VerifyAttachmentConfigAsync(),
                await VerifyCloudStorageConfigAsync(),
                await VerifyComplexCalculationFlagAsync(),
                await VerifyVersionSpecificFeaturesAsync()
            };

            var allPassed = results.All(r => r);

            _output.WriteLine($"\n=== Verification Complete: {(allPassed ? "ALL PASSED" : "SOME FAILED")} ===");

            return allPassed;
        }

        /// <summary>
        /// Verify all 7 versions (v1-v7) exist in PlatformObjectTemplate
        /// </summary>
        public async Task<bool> VerifyAllVersionsExistAsync()
        {
            _output.WriteLine("Test 1: Verifying all 7 versions exist...");

            var collection = _database.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var filter = Builders<BsonDocument>.Filter.Regex("tenantId", new BsonRegularExpression($"^{_tenantId}$", "i"));
            var document = await collection.Find(filter).FirstOrDefaultAsync();

            if (document == null)
            {
                _output.WriteLine($"  ❌ Tenant document not found for: {_tenantId}");
                return false;
            }

            if (!document.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
            {
                _output.WriteLine("  ❌ 'environments' field missing");
                return false;
            }

            var environments = environmentsValue.AsBsonDocument;
            var envKeys = environments.Names.ToList();

            if (!envKeys.Any())
            {
                _output.WriteLine("  ❌ No environments found");
                return false;
            }

            // Check first environment for SalesOrder
            var env = environments[envKeys[0]].AsBsonDocument;
            if (!env.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
            {
                _output.WriteLine("  ❌ 'screens' not found in environment");
                return false;
            }

            var screens = screensValue.AsBsonDocument;
            if (!screens.TryGetValue("SalesOrder", out var salesOrderValue) || salesOrderValue.IsBsonNull)
            {
                _output.WriteLine("  ❌ 'SalesOrder' screen not found");
                return false;
            }

            var salesOrder = salesOrderValue.AsBsonDocument;
            var versions = salesOrder.Names
                .Where(n => n.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                .Select(n => n.ToLower())
                .OrderBy(n => n)
                .ToList();

            var expectedVersions = new[] { "v1", "v2", "v3", "v4", "v5", "v6", "v7" };
            var missingVersions = expectedVersions.Except(versions).ToList();
            var extraVersions = versions.Except(expectedVersions).ToList();

            if (missingVersions.Any())
            {
                _output.WriteLine($"  ❌ Missing versions: {string.Join(", ", missingVersions)}");
            }

            if (extraVersions.Any())
            {
                _output.WriteLine($"  ⚠ Extra versions found: {string.Join(", ", extraVersions)}");
            }

            if (!missingVersions.Any())
            {
                _output.WriteLine($"  ✓ All 7 versions found: {string.Join(", ", versions)}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verify schema structure for each version
        /// </summary>
        public async Task<bool> VerifySchemaStructureForAllVersionsAsync()
        {
            _output.WriteLine("\nTest 2: Verifying schema structure for all versions...");

            var collection = _database.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var filter = Builders<BsonDocument>.Filter.Regex("tenantId", new BsonRegularExpression($"^{_tenantId}$", "i"));
            var document = await collection.Find(filter).FirstOrDefaultAsync();

            if (document == null)
            {
                _output.WriteLine("  ❌ Tenant document not found");
                return false;
            }

            var environments = document["environments"].AsBsonDocument;
            var env = environments[environments.Names.First()].AsBsonDocument;
            var screens = env["screens"].AsBsonDocument;
            var salesOrder = screens["SalesOrder"].AsBsonDocument;

            var allValid = true;

            foreach (var version in new[] { "v1", "v2", "v3", "v4", "v5", "v6", "v7" })
            {
                if (!salesOrder.TryGetValue(version, out var versionDoc) || versionDoc.IsBsonNull)
                {
                    _output.WriteLine($"  ❌ {version}: Not found");
                    allValid = false;
                    continue;
                }

                var versionData = versionDoc.AsBsonDocument;

                // Check required fields
                var hasFields = versionData.TryGetValue("fields", out _);
                var hasObjectType = versionData.TryGetValue("objectType", out _);
                var hasVersion = versionData.TryGetValue("version", out _);

                if (!hasFields || !hasObjectType || !hasVersion)
                {
                    _output.WriteLine($"  ❌ {version}: Missing required fields");
                    allValid = false;
                }
                else
                {
                    _output.WriteLine($"  ✓ {version}: Schema structure valid");
                }
            }

            return allValid;
        }

        /// <summary>
        /// Verify calculationRules configuration exists
        /// </summary>
        public async Task<bool> VerifyCalculationRulesConfigAsync()
        {
            _output.WriteLine("\nTest 3: Verifying calculationRules configuration...");

            var collection = _database.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var filter = Builders<BsonDocument>.Filter.Regex("tenantId", new BsonRegularExpression($"^{_tenantId}$", "i"));
            var document = await collection.Find(filter).FirstOrDefaultAsync();

            if (document == null) return false;

            var environments = document["environments"].AsBsonDocument;
            var env = environments[environments.Names.First()].AsBsonDocument;
            var screens = env["screens"].AsBsonDocument;
            var salesOrder = screens["SalesOrder"].AsBsonDocument;

            var allValid = true;

            foreach (var version in new[] { "v1", "v2", "v3", "v4", "v5", "v6", "v7" })
            {
                if (!salesOrder.TryGetValue(version, out var versionDoc) || versionDoc.IsBsonNull)
                    continue;

                var versionData = versionDoc.AsBsonDocument;

                if (!versionData.TryGetValue("calculationRules", out var calcRules) || calcRules.IsBsonNull)
                {
                    _output.WriteLine($"  ❌ {version}: calculationRules missing");
                    allValid = false;
                    continue;
                }

                var calcRulesDoc = calcRules.AsBsonDocument;

                // Check serverSide structure
                if (!calcRulesDoc.TryGetValue("serverSide", out var serverSide) || serverSide.IsBsonNull)
                {
                    _output.WriteLine($"  ❌ {version}: serverSide missing");
                    allValid = false;
                    continue;
                }

                var serverSideDoc = serverSide.AsBsonDocument;

                // Check required arrays
                var hasLineItemCalcs = serverSideDoc.TryGetValue("lineItemCalculations", out _);
                var hasDocCalcs = serverSideDoc.TryGetValue("documentCalculations", out _);
                var hasComplexCalcs = serverSideDoc.TryGetValue("complexCalculations", out _);

                if (!hasLineItemCalcs || !hasDocCalcs || !hasComplexCalcs)
                {
                    _output.WriteLine($"  ❌ {version}: Missing calculation arrays");
                    allValid = false;
                }
                else
                {
                    _output.WriteLine($"  ✓ {version}: calculationRules valid");
                }
            }

            return allValid;
        }

        /// <summary>
        /// Verify documentTotals configuration exists
        /// </summary>
        public async Task<bool> VerifyDocumentTotalsConfigAsync()
        {
            _output.WriteLine("\nTest 4: Verifying documentTotals configuration...");

            var collection = _database.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var filter = Builders<BsonDocument>.Filter.Regex("tenantId", new BsonRegularExpression($"^{_tenantId}$", "i"));
            var document = await collection.Find(filter).FirstOrDefaultAsync();

            if (document == null) return false;

            var environments = document["environments"].AsBsonDocument;
            var env = environments[environments.Names.First()].AsBsonDocument;
            var screens = env["screens"].AsBsonDocument;
            var salesOrder = screens["SalesOrder"].AsBsonDocument;

            var allValid = true;

            // V1 and V2 should have documentTotals
            foreach (var version in new[] { "v1", "v2" })
            {
                if (!salesOrder.TryGetValue(version, out var versionDoc) || versionDoc.IsBsonNull)
                    continue;

                var versionData = versionDoc.AsBsonDocument;

                if (!versionData.TryGetValue("documentTotals", out var docTotals) || docTotals.IsBsonNull)
                {
                    _output.WriteLine($"  ❌ {version}: documentTotals missing (expected for V1-V2)");
                    allValid = false;
                    continue;
                }

                var docTotalsDoc = docTotals.AsBsonDocument;

                if (!docTotalsDoc.TryGetValue("fields", out _) || !docTotalsDoc.TryGetValue("displayConfig", out _))
                {
                    _output.WriteLine($"  ❌ {version}: documentTotals structure invalid");
                    allValid = false;
                }
                else
                {
                    _output.WriteLine($"  ✓ {version}: documentTotals valid");
                }
            }

            // V3-V7 should NOT have documentTotals (or have it null/empty)
            foreach (var version in new[] { "v3", "v4", "v5", "v6", "v7" })
            {
                if (!salesOrder.TryGetValue(version, out var versionDoc) || versionDoc.IsBsonNull)
                    continue;

                var versionData = versionDoc.AsBsonDocument;

                if (versionData.TryGetValue("documentTotals", out var docTotals) && !docTotals.IsBsonNull)
                {
                    _output.WriteLine($"  ⚠ {version}: documentTotals present (should be disabled for V3-V7)");
                }
                else
                {
                    _output.WriteLine($"  ✓ {version}: documentTotals correctly disabled");
                }
            }

            return allValid;
        }

        /// <summary>
        /// Verify attachmentConfig configuration exists
        /// </summary>
        public async Task<bool> VerifyAttachmentConfigAsync()
        {
            _output.WriteLine("\nTest 5: Verifying attachmentConfig configuration...");

            var collection = _database.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var filter = Builders<BsonDocument>.Filter.Regex("tenantId", new BsonRegularExpression($"^{_tenantId}$", "i"));
            var document = await collection.Find(filter).FirstOrDefaultAsync();

            if (document == null) return false;

            var environments = document["environments"].AsBsonDocument;
            var env = environments[environments.Names.First()].AsBsonDocument;
            var screens = env["screens"].AsBsonDocument;
            var salesOrder = screens["SalesOrder"].AsBsonDocument;

            var allValid = true;

            // V1 and V3 should have attachments
            foreach (var version in new[] { "v1", "v3" })
            {
                if (!salesOrder.TryGetValue(version, out var versionDoc) || versionDoc.IsBsonNull)
                    continue;

                var versionData = versionDoc.AsBsonDocument;

                if (!versionData.TryGetValue("attachmentConfig", out var attachConfig) || attachConfig.IsBsonNull)
                {
                    _output.WriteLine($"  ❌ {version}: attachmentConfig missing");
                    allValid = false;
                    continue;
                }

                var attachDoc = attachConfig.AsBsonDocument;

                if (!attachDoc.TryGetValue("documentLevel", out _) || !attachDoc.TryGetValue("lineLevel", out _))
                {
                    _output.WriteLine($"  ❌ {version}: attachmentConfig structure invalid");
                    allValid = false;
                }
                else
                {
                    _output.WriteLine($"  ✓ {version}: attachmentConfig valid");
                }
            }

            // V2, V4-V7 should NOT have attachments
            foreach (var version in new[] { "v2", "v4", "v5", "v6", "v7" })
            {
                if (!salesOrder.TryGetValue(version, out var versionDoc) || versionDoc.IsBsonNull)
                    continue;

                var versionData = versionDoc.AsBsonDocument;

                if (versionData.TryGetValue("attachmentConfig", out var attachConfig) && !attachConfig.IsBsonNull)
                {
                    _output.WriteLine($"  ⚠ {version}: attachmentConfig present (should be disabled)");
                }
                else
                {
                    _output.WriteLine($"  ✓ {version}: attachmentConfig correctly disabled");
                }
            }

            return allValid;
        }

        /// <summary>
        /// Verify cloudStorage configuration exists
        /// </summary>
        public async Task<bool> VerifyCloudStorageConfigAsync()
        {
            _output.WriteLine("\nTest 6: Verifying cloudStorage configuration...");

            var collection = _database.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var filter = Builders<BsonDocument>.Filter.Regex("tenantId", new BsonRegularExpression($"^{_tenantId}$", "i"));
            var document = await collection.Find(filter).FirstOrDefaultAsync();

            if (document == null) return false;

            var environments = document["environments"].AsBsonDocument;
            var env = environments[environments.Names.First()].AsBsonDocument;
            var screens = env["screens"].AsBsonDocument;
            var salesOrder = screens["SalesOrder"].AsBsonDocument;

            var allValid = true;

            // V1 and V4 should have cloud storage
            foreach (var version in new[] { "v1", "v4" })
            {
                if (!salesOrder.TryGetValue(version, out var versionDoc) || versionDoc.IsBsonNull)
                    continue;

                var versionData = versionDoc.AsBsonDocument;

                if (!versionData.TryGetValue("cloudStorage", out var cloudStorage) || cloudStorage.IsBsonNull)
                {
                    _output.WriteLine($"  ❌ {version}: cloudStorage missing");
                    allValid = false;
                    continue;
                }

                var cloudDoc = cloudStorage.AsBsonDocument;

                if (!cloudDoc.TryGetValue("providers", out _) || !cloudDoc.TryGetValue("globalSettings", out _))
                {
                    _output.WriteLine($"  ❌ {version}: cloudStorage structure invalid");
                    allValid = false;
                }
                else
                {
                    _output.WriteLine($"  ✓ {version}: cloudStorage valid");
                }
            }

            // V2-V3, V5-V7 should NOT have cloud storage
            foreach (var version in new[] { "v2", "v3", "v5", "v6", "v7" })
            {
                if (!salesOrder.TryGetValue(version, out var versionDoc) || versionDoc.IsBsonNull)
                    continue;

                var versionData = versionDoc.AsBsonDocument;

                if (versionData.TryGetValue("cloudStorage", out var cloudStorage) && !cloudStorage.IsBsonNull)
                {
                    _output.WriteLine($"  ⚠ {version}: cloudStorage present (should be disabled)");
                }
                else
                {
                    _output.WriteLine($"  ✓ {version}: cloudStorage correctly disabled");
                }
            }

            return allValid;
        }

        /// <summary>
        /// Verify complexCalculation flag behavior
        /// </summary>
        public async Task<bool> VerifyComplexCalculationFlagAsync()
        {
            _output.WriteLine("\nTest 7: Verifying complexCalculation flag...");

            var collection = _database.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var filter = Builders<BsonDocument>.Filter.Regex("tenantId", new BsonRegularExpression($"^{_tenantId}$", "i"));
            var document = await collection.Find(filter).FirstOrDefaultAsync();

            if (document == null) return false;

            var environments = document["environments"].AsBsonDocument;
            var env = environments[environments.Names.First()].AsBsonDocument;
            var screens = env["screens"].AsBsonDocument;
            var salesOrder = screens["SalesOrder"].AsBsonDocument;

            var allValid = true;

            // V1 should have complexCalculation = true
            if (salesOrder.TryGetValue("v1", out var v1Doc) && !v1Doc.IsBsonNull)
            {
                var v1Data = v1Doc.AsBsonDocument;
                if (v1Data.TryGetValue("calculationRules", out var calcRules) && !calcRules.IsBsonNull)
                {
                    var calcDoc = calcRules.AsBsonDocument;
                    if (calcDoc.TryGetValue("complexCalculation", out var ccFlag))
                    {
                        var isEnabled = ccFlag.IsBoolean ? ccFlag.AsBoolean : ccFlag.AsInt32 != 0;
                        if (isEnabled)
                        {
                            _output.WriteLine("  ✓ V1: complexCalculation = true");
                        }
                        else
                        {
                            _output.WriteLine("  ❌ V1: complexCalculation should be true");
                            allValid = false;
                        }
                    }
                    else
                    {
                        _output.WriteLine("  ❌ V1: complexCalculation flag missing");
                        allValid = false;
                    }
                }
            }

            // V2-V7 should have complexCalculation = false
            foreach (var version in new[] { "v2", "v3", "v4", "v5", "v6", "v7" })
            {
                if (!salesOrder.TryGetValue(version, out var versionDoc) || versionDoc.IsBsonNull)
                    continue;

                var versionData = versionDoc.AsBsonDocument;
                if (versionData.TryGetValue("calculationRules", out var calcRules) && !calcRules.IsBsonNull)
                {
                    var calcDoc = calcRules.AsBsonDocument;
                    if (calcDoc.TryGetValue("complexCalculation", out var ccFlag))
                    {
                        var isEnabled = ccFlag.IsBoolean ? ccFlag.AsBoolean : ccFlag.AsInt32 != 0;
                        if (!isEnabled)
                        {
                            _output.WriteLine($"  ✓ {version}: complexCalculation = false");
                        }
                        else
                        {
                            _output.WriteLine($"  ❌ {version}: complexCalculation should be false");
                            allValid = false;
                        }
                    }
                }
            }

            return allValid;
        }

        /// <summary>
        /// Verify version-specific feature flags
        /// </summary>
        public async Task<bool> VerifyVersionSpecificFeaturesAsync()
        {
            _output.WriteLine("\nTest 8: Verifying version-specific feature flags...");

            var collection = _database.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var filter = Builders<BsonDocument>.Filter.Regex("tenantId", new BsonRegularExpression($"^{_tenantId}$", "i"));
            var document = await collection.Find(filter).FirstOrDefaultAsync();

            if (document == null) return false;

            var environments = document["environments"].AsBsonDocument;
            var env = environments[environments.Names.First()].AsBsonDocument;
            var screens = env["screens"].AsBsonDocument;
            var salesOrder = screens["SalesOrder"].AsBsonDocument;

            _output.WriteLine("  Version Feature Matrix:");
            _output.WriteLine("  Version | Complex Calc | Doc Totals | Attachments | Cloud Storage");
            _output.WriteLine("  --------|--------------|------------|-------------|--------------");

            foreach (var version in new[] { "v1", "v2", "v3", "v4", "v5", "v6", "v7" })
            {
                if (!salesOrder.TryGetValue(version, out var versionDoc) || versionDoc.IsBsonNull)
                    continue;

                var versionData = versionDoc.AsBsonDocument;

                // Check features
                var hasComplexCalc = false;
                if (versionData.TryGetValue("calculationRules", out var calcRules) && !calcRules.IsBsonNull)
                {
                    var calcDoc = calcRules.AsBsonDocument;
                    if (calcDoc.TryGetValue("complexCalculation", out var ccFlag))
                    {
                        hasComplexCalc = ccFlag.IsBoolean ? ccFlag.AsBoolean : ccFlag.AsInt32 != 0;
                    }
                }

                var hasDocTotals = versionData.TryGetValue("documentTotals", out var dt) && !dt.IsBsonNull;
                var hasAttachments = versionData.TryGetValue("attachmentConfig", out var ac) && !ac.IsBsonNull;
                var hasCloudStorage = versionData.TryGetValue("cloudStorage", out var cs) && !cs.IsBsonNull;

                _output.WriteLine($"  {version,-7} | { (hasComplexCalc ? "✓" : "✗"),-12} | { (hasDocTotals ? "✓" : "✗"),-10} | { (hasAttachments ? "✓" : "✗"),-11} | { (hasCloudStorage ? "✓" : "✗")}");
            }

            _output.WriteLine("\n  Expected:");
            _output.WriteLine("  V1: ✓ Complex Calc, ✓ Doc Totals, ✓ Attachments, ✓ Cloud Storage");
            _output.WriteLine("  V2: ✗ Complex Calc, ✓ Doc Totals, ✗ Attachments, ✗ Cloud Storage");
            _output.WriteLine("  V3: ✗ Complex Calc, ✗ Doc Totals, ✓ Attachments, ✗ Cloud Storage");
            _output.WriteLine("  V4: ✗ Complex Calc, ✗ Doc Totals, ✗ Attachments, ✓ Cloud Storage");
            _output.WriteLine("  V5-V7: ✗ All features");

            return true;
        }

        /// <summary>
        /// Print detailed schema information for debugging
        /// </summary>
        public async Task PrintSchemaDetailsAsync(string version)
        {
            _output.WriteLine($"\n=== Schema Details for {version} ===");

            var collection = _database.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var filter = Builders<BsonDocument>.Filter.Regex("tenantId", new BsonRegularExpression($"^{_tenantId}$", "i"));
            var document = await collection.Find(filter).FirstOrDefaultAsync();

            if (document == null)
            {
                _output.WriteLine("Tenant document not found");
                return;
            }

            var environments = document["environments"].AsBsonDocument;
            var env = environments[environments.Names.First()].AsBsonDocument;
            var screens = env["screens"].AsBsonDocument;
            var salesOrder = screens["SalesOrder"].AsBsonDocument;

            if (!salesOrder.TryGetValue(version, out var versionDoc) || versionDoc.IsBsonNull)
            {
                _output.WriteLine($"Version {version} not found");
                return;
            }

            var versionData = versionDoc.AsBsonDocument;
            var json = versionData.ToJson(new JsonWriterSettings { Indent = true });
            _output.WriteLine(json);
        }
    }
}
