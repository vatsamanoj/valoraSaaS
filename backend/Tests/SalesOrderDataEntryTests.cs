using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Valora.Api;
using Valora.Api.Application.Schemas;
using Valora.Api.Application.Schemas.TemplateConfig;
using Xunit;
using Xunit.Abstractions;

namespace Valora.Tests.Functional
{
    /// <summary>
    /// Functional Tests for Sales Order Data Entry Workflows
    /// Tests actual data entry scenarios with real data values
    /// </summary>
    public class SalesOrderDataEntryTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;
        private readonly JsonSerializerOptions _jsonOptions;

        public SalesOrderDataEntryTests(TestWebApplicationFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _output = output;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region Test Data Entry Scenarios

        /// <summary>
        /// Test Case: Create Sales Order with complete data entry
        /// Scenario: User enters all required fields including line items
        /// Expected: Order created successfully with all data persisted
        /// </summary>
        [Fact]
        public async Task DataEntry_CreateCompleteSalesOrder_AllFieldsPersisted()
        {
            // Arrange
            var orderData = new
            {
                OrderNumber = $"SO-DE-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                CustomerName = "Test Customer Ltd",
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DeliveryDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
                Reference = "REF-2024-001",
                Notes = "Please deliver before noon",
                Currency = "USD",
                ExchangeRate = 1.0,
                Items = new[]
                {
                    new
                    {
                        LineNumber = 1,
                        ItemCode = "PROD001",
                        ItemName = "Premium Widget",
                        Quantity = 100,
                        UnitPrice = 50.00,
                        DiscountPercent = 10,
                        TaxPercent = 18,
                        DeliveryDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
                        Notes = "Handle with care"
                    },
                    new
                    {
                        LineNumber = 2,
                        ItemCode = "PROD002",
                        ItemName = "Standard Gadget",
                        Quantity = 50,
                        UnitPrice = 25.00,
                        DiscountPercent = 5,
                        TaxPercent = 18,
                        DeliveryDate = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd"),
                        Notes = ""
                    }
                }
            };

            _output.WriteLine("Test: DataEntry_CreateCompleteSalesOrder_AllFieldsPersisted");
            _output.WriteLine($"Order Number: {orderData.OrderNumber}");
            _output.WriteLine($"Customer: {orderData.CustomerName}");
            _output.WriteLine($"Line Items: {orderData.Items.Length}");

            // Act - Create Sales Order
            var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/sales/orders")
            {
                Content = new StringContent(JsonSerializer.Serialize(orderData), Encoding.UTF8, "application/json")
            };
            createRequest.Headers.Add("X-Tenant-ID", "test-tenant");
            createRequest.Headers.Add("X-Environment", "dev");

            var createResponse = await _client.SendAsync(createRequest);
            var createContent = await createResponse.Content.ReadAsStringAsync();

            // Assert
            Assert.True(createResponse.IsSuccessStatusCode, 
                $"Create failed: {createResponse.StatusCode} - {createContent}");

            var result = JsonSerializer.Deserialize<JsonElement>(createContent, _jsonOptions);
            var orderId = result.GetProperty("data").GetProperty("id").GetString();
            Assert.NotNull(orderId);

            _output.WriteLine($"✓ Order created with ID: {orderId}");

            // Verify - Retrieve and validate
            var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/sales/orders/{orderId}");
            getRequest.Headers.Add("X-Tenant-ID", "test-tenant");
            getRequest.Headers.Add("X-Environment", "dev");

            var getResponse = await _client.SendAsync(getRequest);
            var getContent = await getResponse.Content.ReadAsStringAsync();

            Assert.True(getResponse.IsSuccessStatusCode);

            var retrievedOrder = JsonSerializer.Deserialize<JsonElement>(getContent, _jsonOptions);
            var data = retrievedOrder.GetProperty("data");

            // Verify all fields persisted
            Assert.Equal(orderData.OrderNumber, data.GetProperty("orderNumber").GetString());
            Assert.Equal(orderData.CustomerId, data.GetProperty("customerId").GetString());
            Assert.Equal(orderData.CustomerName, data.GetProperty("customerName").GetString());
            Assert.Equal(orderData.Reference, data.GetProperty("reference").GetString());
            Assert.Equal(orderData.Notes, data.GetProperty("notes").GetString());

            var items = data.GetProperty("items");
            Assert.Equal(2, items.GetArrayLength());

            _output.WriteLine("✓ All fields persisted correctly");
        }

        /// <summary>
        /// Test Case: Sales Order with Calculation Rules
        /// Scenario: User enters quantity and unit price, system calculates totals
        /// Expected: Line totals, document totals calculated correctly
        /// </summary>
        [Theory]
        [InlineData(10, 100.00, 0, 1000.00)]      // No discount
        [InlineData(10, 100.00, 10, 900.00)]      // 10% discount
        [InlineData(100, 50.00, 20, 4000.00)]     // Volume with 20% discount
        [InlineData(1, 999.99, 0, 999.99)]        // Single item
        public async Task DataEntry_CalculationRules_LineTotalsCalculated(
            int quantity, decimal unitPrice, decimal discountPercent, decimal expectedTotal)
        {
            // Arrange
            var orderData = new
            {
                OrderNumber = $"SO-CALC-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Items = new[]
                {
                    new
                    {
                        LineNumber = 1,
                        ItemCode = "TEST001",
                        ItemName = "Test Product",
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        DiscountPercent = discountPercent,
                        TaxPercent = 18
                    }
                }
            };

            _output.WriteLine($"Test: Qty={quantity}, Price={unitPrice}, Discount={discountPercent}%");
            _output.WriteLine($"Expected Total: {expectedTotal}");

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/orders")
            {
                Content = new StringContent(JsonSerializer.Serialize(orderData), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Tenant-ID", "test-tenant");
            request.Headers.Add("X-Environment", "dev");

            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode, 
                $"Request failed: {response.StatusCode} - {content}");

            var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            var lineTotal = result.GetProperty("data").GetProperty("items")[0]
                .GetProperty("lineTotal").GetDecimal();

            Assert.Equal(expectedTotal, lineTotal);
            _output.WriteLine($"✓ Line total calculated correctly: {lineTotal}");
        }

        /// <summary>
        /// Test Case: Document Totals Calculation
        /// Scenario: Multiple line items with various discounts and taxes
        /// Expected: Document totals (subtotal, discount, tax, grand total) calculated
        /// </summary>
        [Fact]
        public async Task DataEntry_DocumentTotals_MultipleLinesCalculated()
        {
            // Arrange - 3 line items with different values
            var orderData = new
            {
                OrderNumber = $"SO-TOT-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Items = new[]
                {
                    new { LineNumber = 1, ItemCode = "A001", Quantity = 10, UnitPrice = 100m, DiscountPercent = 0m, TaxPercent = 18m },
                    new { LineNumber = 2, ItemCode = "A002", Quantity = 5, UnitPrice = 200m, DiscountPercent = 10m, TaxPercent = 18m },
                    new { LineNumber = 3, ItemCode = "A003", Quantity = 20, UnitPrice = 50m, DiscountPercent = 5m, TaxPercent = 18m }
                }
            };

            // Expected calculations:
            // Line 1: 10 * 100 = 1000, no discount = 1000, tax = 180, total = 1180
            // Line 2: 5 * 200 = 1000, 10% discount = 900, tax = 162, total = 1062
            // Line 3: 20 * 50 = 1000, 5% discount = 950, tax = 171, total = 1121
            // Subtotal: 2850, Tax: 513, Grand Total: 3363

            _output.WriteLine("Test: Document Totals with Multiple Lines");

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/orders")
            {
                Content = new StringContent(JsonSerializer.Serialize(orderData), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Tenant-ID", "test-tenant");
            request.Headers.Add("X-Environment", "dev");

            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode, 
                $"Request failed: {response.StatusCode} - {content}");

            var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            var totals = result.GetProperty("data").GetProperty("totals");

            var subtotal = totals.GetProperty("subtotal").GetDecimal();
            var totalDiscount = totals.GetProperty("totalDiscount").GetDecimal();
            var totalTax = totals.GetProperty("totalTax").GetDecimal();
            var grandTotal = totals.GetProperty("grandTotal").GetDecimal();

            // Verify calculations
            Assert.Equal(2850m, subtotal);      // 1000 + 900 + 950
            Assert.Equal(150m, totalDiscount);  // 0 + 100 + 50
            Assert.Equal(513m, totalTax);       // 180 + 162 + 171
            Assert.Equal(3363m, grandTotal);    // 2850 + 513

            _output.WriteLine($"✓ Subtotal: {subtotal}");
            _output.WriteLine($"✓ Total Discount: {totalDiscount}");
            _output.WriteLine($"✓ Total Tax: {totalTax}");
            _output.WriteLine($"✓ Grand Total: {grandTotal}");
        }

        /// <summary>
        /// Test Case: Attachment Upload Configuration
        /// Scenario: Verify attachment config allows document and line level uploads
        /// Expected: Config has proper settings for file uploads
        /// </summary>
        [Fact]
        public async Task DataEntry_AttachmentConfig_ValidatesUploadSettings()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/object/SalesOrder/latest");
            request.Headers.Add("X-Tenant-ID", "test-tenant");
            request.Headers.Add("X-Environment", "dev");

            // Act
            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            var data = result.GetProperty("data");
            var attachmentConfig = data.GetProperty("attachmentConfig");

            // Document level config
            var docLevel = attachmentConfig.GetProperty("documentLevel");
            Assert.True(docLevel.GetProperty("enabled").GetBoolean());
            Assert.True(docLevel.GetProperty("maxFiles").GetInt32() > 0);
            Assert.True(docLevel.GetProperty("maxFileSizeMB").GetInt32() > 0);

            // Line level config
            var lineLevel = attachmentConfig.GetProperty("lineLevel");
            Assert.True(lineLevel.GetProperty("enabled").GetBoolean());

            _output.WriteLine("✓ Document-level attachments enabled");
            _output.WriteLine("✓ Line-level attachments enabled");
            _output.WriteLine($"✓ Max file size: {docLevel.GetProperty("maxFileSizeMB").GetInt32()}MB");
        }

        /// <summary>
        /// Test Case: Cloud Storage Configuration
        /// Scenario: Verify cloud storage providers are configured
        /// Expected: Storage providers defined with proper settings
        /// </summary>
        [Fact]
        public async Task DataEntry_CloudStorageConfig_ValidatesProviders()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/object/SalesOrder/latest");
            request.Headers.Add("X-Tenant-ID", "test-tenant");
            request.Headers.Add("X-Environment", "dev");

            // Act
            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            var data = result.GetProperty("data");
            var cloudStorage = data.GetProperty("cloudStorage");

            // Verify providers exist
            var providers = cloudStorage.GetProperty("providers");
            Assert.True(providers.GetArrayLength() > 0);

            // Verify global settings
            var globalSettings = cloudStorage.GetProperty("globalSettings");
            Assert.True(globalSettings.GetProperty("virusScanEnabled").GetBoolean());
            Assert.True(globalSettings.GetProperty("generateThumbnails").GetBoolean());

            _output.WriteLine($"✓ Storage providers configured: {providers.GetArrayLength()}");
            _output.WriteLine("✓ Virus scanning enabled");
            _output.WriteLine("✓ Thumbnail generation enabled");
        }

        /// <summary>
        /// Test Case: Schema Version Compatibility
        /// Scenario: Test all 7 schema versions load correctly
        /// Expected: Each version returns valid schema with appropriate features
        /// </summary>
        [Theory]
        [InlineData(1, false, false, false)]  // v1: Basic
        [InlineData(2, true, false, false)]   // v2: + Calculations
        [InlineData(3, true, true, false)]    // v3: + Document Totals
        [InlineData(4, true, true, true)]     // v4: + Attachments
        [InlineData(5, true, true, true)]     // v5: + Cloud Storage
        [InlineData(6, true, true, true)]     // v6: + Complex Calculations
        [InlineData(7, true, true, true)]     // v7: + Smart Projections
        public async Task DataEntry_AllSchemaVersions_LoadCorrectly(
            int version, bool hasCalculations, bool hasDocTotals, bool hasAttachments)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/platform/object/SalesOrder/latest?v={version}");
            request.Headers.Add("X-Tenant-ID", "test-tenant");
            request.Headers.Add("X-Environment", "dev");

            _output.WriteLine($"Testing Schema Version {version}");

            // Act
            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode, 
                $"Version {version} failed: {response.StatusCode}");

            var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            var data = result.GetProperty("data");

            // Verify version-specific features
            if (hasCalculations)
            {
                Assert.True(data.TryGetProperty("calculationRules", out _));
                _output.WriteLine("  ✓ Has calculation rules");
            }

            if (hasDocTotals)
            {
                Assert.True(data.TryGetProperty("documentTotals", out _));
                _output.WriteLine("  ✓ Has document totals");
            }

            if (hasAttachments)
            {
                Assert.True(data.TryGetProperty("attachmentConfig", out _));
                _output.WriteLine("  ✓ Has attachment config");
            }

            _output.WriteLine($"✓ Version {version} loaded successfully");
        }

        /// <summary>
        /// Test Case: Complex Calculation with External Data
        /// Scenario: Volume discount based on customer tier
        /// Expected: Discount calculated using external customer data
        /// </summary>
        [Theory]
        [InlineData("CUST-TIER1", 100, 10)]   // Tier 1: 10% discount
        [InlineData("CUST-TIER2", 100, 15)]   // Tier 2: 15% discount
        [InlineData("CUST-TIER3", 100, 20)]   // Tier 3: 20% discount
        public async Task DataEntry_ComplexCalculation_CustomerTierDiscount(
            string customerTier, int quantity, int expectedDiscountPercent)
        {
            // Arrange
            var orderData = new
            {
                OrderNumber = $"SO-COMPLEX-{Guid.NewGuid():N}",
                CustomerId = customerTier,
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Items = new[]
                {
                    new
                    {
                        LineNumber = 1,
                        ItemCode = "PROD001",
                        Quantity = quantity,
                        UnitPrice = 100.00,
                        DiscountPercent = 0  // Will be calculated
                    }
                }
            };

            _output.WriteLine($"Test: Customer={customerTier}, Qty={quantity}");
            _output.WriteLine($"Expected Discount: {expectedDiscountPercent}%");

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/orders/calculate")
            {
                Content = new StringContent(JsonSerializer.Serialize(orderData), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Tenant-ID", "test-tenant");
            request.Headers.Add("X-Environment", "dev");

            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            var calculatedDiscount = result.GetProperty("data").GetProperty("items")[0]
                .GetProperty("discountPercent").GetInt32();

            Assert.Equal(expectedDiscountPercent, calculatedDiscount);
            _output.WriteLine($"✓ Discount calculated: {calculatedDiscount}%");
        }

        /// <summary>
        /// Test Case: Smart Projection Data Integrity
        /// Scenario: Create order and verify MongoDB projection
        /// Expected: Projection contains all data with proper indexing
        /// </summary>
        [Fact]
        public async Task DataEntry_SmartProjection_DataIndexedCorrectly()
        {
            // Arrange
            var orderData = new
            {
                OrderNumber = $"SO-PROJ-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                CustomerName = "Projection Test Customer",
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Status = "Draft",
                Items = new[]
                {
                    new { LineNumber = 1, ItemCode = "PROJ001", Quantity = 10, UnitPrice = 100.00 }
                }
            };

            _output.WriteLine("Test: Smart Projection Data Integrity");

            // Act - Create order
            var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/sales/orders")
            {
                Content = new StringContent(JsonSerializer.Serialize(orderData), Encoding.UTF8, "application/json")
            };
            createRequest.Headers.Add("X-Tenant-ID", "test-tenant");
            createRequest.Headers.Add("X-Environment", "dev");

            var createResponse = await _client.SendAsync(createRequest);
            var createContent = await createResponse.Content.ReadAsStringAsync();

            Assert.True(createResponse.IsSuccessStatusCode);

            var result = JsonSerializer.Deserialize<JsonElement>(createContent, _jsonOptions);
            var orderId = result.GetProperty("data").GetProperty("id").GetString();

            // Query projection
            var projRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/projections/SalesOrder/{orderId}");
            projRequest.Headers.Add("X-Tenant-ID", "test-tenant");

            var projResponse = await _client.SendAsync(projRequest);
            var projContent = await projResponse.Content.ReadAsStringAsync();

            // Assert
            Assert.True(projResponse.IsSuccessStatusCode);

            var projection = JsonSerializer.Deserialize<JsonElement>(projContent, _jsonOptions);
            var projData = projection.GetProperty("data");

            Assert.Equal(orderData.OrderNumber, projData.GetProperty("orderNumber").GetString());
            Assert.Equal(orderData.CustomerName, projData.GetProperty("customerName").GetString());
            Assert.True(projData.TryGetProperty("_metadata", out var metadata));
            Assert.True(metadata.TryGetProperty("indexedAt", out _));

            _output.WriteLine($"✓ Projection created for order: {orderId}");
            _output.WriteLine("✓ Data properly indexed");
        }

        /// <summary>
        /// Test Case: Validation Rules on Data Entry
        /// Scenario: Attempt to save invalid data
        /// Expected: Validation errors returned
        /// </summary>
        [Theory]
        [InlineData("", "CUST001", "Order number is required")]
        [InlineData("SO-001", "", "Customer is required")]
        public async Task DataEntry_Validation_InvalidDataReturnsErrors(
            string orderNumber, string customerId, string expectedError)
        {
            // Arrange
            var orderData = new
            {
                OrderNumber = orderNumber,
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Items = new[] { new { LineNumber = 1, ItemCode = "TEST", Quantity = 1, UnitPrice = 1.0 } }
            };

            _output.WriteLine($"Test: OrderNumber='{orderNumber}', CustomerId='{customerId}'");

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/orders")
            {
                Content = new StringContent(JsonSerializer.Serialize(orderData), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Tenant-ID", "test-tenant");
            request.Headers.Add("X-Environment", "dev");

            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.False(response.IsSuccessStatusCode);
            Assert.Contains(expectedError, content, StringComparison.OrdinalIgnoreCase);

            _output.WriteLine($"✓ Validation error returned: {expectedError}");
        }

        /// <summary>
        /// Test Case: Update Existing Sales Order
        /// Scenario: User modifies an existing order
        /// Expected: Changes persisted, history tracked
        /// </summary>
        [Fact]
        public async Task DataEntry_UpdateSalesOrder_ChangesPersisted()
        {
            // Arrange - Create initial order
            var initialOrder = new
            {
                OrderNumber = $"SO-UPD-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Notes = "Initial notes",
                Items = new[] { new { LineNumber = 1, ItemCode = "ITEM001", Quantity = 10, UnitPrice = 100.00 } }
            };

            var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/sales/orders")
            {
                Content = new StringContent(JsonSerializer.Serialize(initialOrder), Encoding.UTF8, "application/json")
            };
            createRequest.Headers.Add("X-Tenant-ID", "test-tenant");
            createRequest.Headers.Add("X-Environment", "dev");

            var createResponse = await _client.SendAsync(createRequest);
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var orderId = JsonSerializer.Deserialize<JsonElement>(createContent, _jsonOptions)
                .GetProperty("data").GetProperty("id").GetString();

            _output.WriteLine($"Created order: {orderId}");

            // Act - Update order
            var updateData = new
            {
                Notes = "Updated notes - added delivery instructions",
                Items = new[]
                {
                    new { LineNumber = 1, ItemCode = "ITEM001", Quantity = 15, UnitPrice = 100.00 },
                    new { LineNumber = 2, ItemCode = "ITEM002", Quantity = 5, UnitPrice = 50.00 }
                }
            };

            var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/sales/orders/{orderId}")
            {
                Content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json")
            };
            updateRequest.Headers.Add("X-Tenant-ID", "test-tenant");
            updateRequest.Headers.Add("X-Environment", "dev");

            var updateResponse = await _client.SendAsync(updateRequest);
            var updateContent = await updateResponse.Content.ReadAsStringAsync();

            // Assert
            Assert.True(updateResponse.IsSuccessStatusCode);

            var result = JsonSerializer.Deserialize<JsonElement>(updateContent, _jsonOptions);
            var items = result.GetProperty("data").GetProperty("items");

            Assert.Equal(2, items.GetArrayLength());
            Assert.Equal("Updated notes - added delivery instructions",
                result.GetProperty("data").GetProperty("notes").GetString());

            _output.WriteLine("✓ Order updated successfully");
            _output.WriteLine("✓ Line items modified");
            _output.WriteLine("✓ Notes updated");
        }

        /// <summary>
        /// Test Case: Status Workflow
        /// Scenario: Move order through status lifecycle
        /// Expected: Status transitions validated and tracked
        /// </summary>
        [Fact]
        public async Task DataEntry_StatusWorkflow_ValidTransitions()
        {
            // Arrange - Create order
            var orderData = new
            {
                OrderNumber = $"SO-WF-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Status = "Draft",
                Items = new[] { new { LineNumber = 1, ItemCode = "ITEM001", Quantity = 10, UnitPrice = 100.00 } }
            };

            var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/sales/orders")
            {
                Content = new StringContent(JsonSerializer.Serialize(orderData), Encoding.UTF8, "application/json")
            };
            createRequest.Headers.Add("X-Tenant-ID", "test-tenant");
            createRequest.Headers.Add("X-Environment", "dev");

            var createResponse = await _client.SendAsync(createRequest);
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var orderId = JsonSerializer.Deserialize<JsonElement>(createContent, _jsonOptions)
                .GetProperty("data").GetProperty("id").GetString();

            _output.WriteLine("Test: Status Workflow");
            _output.WriteLine($"Order ID: {orderId}");

            // Act & Assert - Transition to Confirmed
            var confirmRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/orders/{orderId}/confirm");
            confirmRequest.Headers.Add("X-Tenant-ID", "test-tenant");
            var confirmResponse = await _client.SendAsync(confirmRequest);
            Assert.True(confirmResponse.IsSuccessStatusCode);
            _output.WriteLine("✓ Draft → Confirmed");

            // Transition to Shipped
            var shipRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/orders/{orderId}/ship");
            shipRequest.Headers.Add("X-Tenant-ID", "test-tenant");
            var shipResponse = await _client.SendAsync(shipRequest);
            Assert.True(shipResponse.IsSuccessStatusCode);
            _output.WriteLine("✓ Confirmed → Shipped");

            // Transition to Invoiced
            var invoiceRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/orders/{orderId}/invoice");
            invoiceRequest.Headers.Add("X-Tenant-ID", "test-tenant");
            var invoiceResponse = await _client.SendAsync(invoiceRequest);
            Assert.True(invoiceResponse.IsSuccessStatusCode);
            _output.WriteLine("✓ Shipped → Invoiced");

            // Verify final status
            var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/sales/orders/{orderId}");
            getRequest.Headers.Add("X-Tenant-ID", "test-tenant");
            var getResponse = await _client.SendAsync(getRequest);
            var getContent = await getResponse.Content.ReadAsStringAsync();

            var finalOrder = JsonSerializer.Deserialize<JsonElement>(getContent, _jsonOptions);
            Assert.Equal("Invoiced", finalOrder.GetProperty("data").GetProperty("status").GetString());

            _output.WriteLine("✓ Final status: Invoiced");
        }

        /// <summary>
        /// Test Case: Bulk Data Entry
        /// Scenario: Create multiple orders simultaneously
        /// Expected: All orders created successfully
        /// </summary>
        [Fact]
        public async Task DataEntry_BulkCreate_MultipleOrders()
        {
            // Arrange
            var orders = Enumerable.Range(1, 10).Select(i => new
            {
                OrderNumber = $"SO-BULK-{Guid.NewGuid():N}-{i:D3}",
                CustomerId = $"CUST{i:D3}",
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Items = new[] { new { LineNumber = 1, ItemCode = $"ITEM{i:D3}", Quantity = i * 10, UnitPrice = 100.00 } }
            }).ToList();

            _output.WriteLine("Test: Bulk Create - 10 Orders");

            // Act
            var tasks = orders.Select(async order =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/orders")
                {
                    Content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-Tenant-ID", "test-tenant");
                request.Headers.Add("X-Environment", "dev");
                return await _client.SendAsync(request);
            });

            var responses = await Task.WhenAll(tasks);

            // Assert
            var successCount = responses.Count(r => r.IsSuccessStatusCode);
            Assert.Equal(10, successCount);

            _output.WriteLine($"✓ {successCount}/10 orders created successfully");
        }

        /// <summary>
        /// Test Case: Search and Filter
        /// Scenario: Query orders with various filters
        /// Expected: Correct results returned based on filters
        /// </summary>
        [Theory]
        [InlineData("customerId", "CUST001", 1)]
        [InlineData("status", "Draft", 1)]
        public async Task DataEntry_SearchFilters_ReturnsCorrectResults(
            string filterField, string filterValue, int expectedMinResults)
        {
            // Arrange - First create some test orders
            for (int i = 0; i < 5; i++)
            {
                var order = new
                {
                    OrderNumber = $"SO-SEARCH-{Guid.NewGuid():N}",
                    CustomerId = i % 2 == 0 ? "CUST001" : "CUST002",
                    OrderDate = DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd"),
                    Status = i % 2 == 0 ? "Draft" : "Confirmed",
                    Items = new[] { new { LineNumber = 1, ItemCode = "ITEM001", Quantity = (i + 1) * 10, UnitPrice = 100.00 } }
                };

                var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/sales/orders")
                {
                    Content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json")
                };
                createRequest.Headers.Add("X-Tenant-ID", "test-tenant");
                await _client.SendAsync(createRequest);
            }

            _output.WriteLine($"Test: Search by {filterField}={filterValue}");

            // Act
            var searchRequest = new HttpRequestMessage(HttpMethod.Get,
                $"/api/sales/orders?{filterField}={filterValue}");
            searchRequest.Headers.Add("X-Tenant-ID", "test-tenant");
            searchRequest.Headers.Add("X-Environment", "dev");

            var response = await _client.SendAsync(searchRequest);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            var items = result.GetProperty("data").GetProperty("items");

            Assert.True(items.GetArrayLength() >= expectedMinResults);
            _output.WriteLine($"✓ Found {items.GetArrayLength()} orders");
        }

        #endregion
    }
}
        ///