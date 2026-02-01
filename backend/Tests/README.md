# Valora Test Suite

This directory contains comprehensive tests for the Valora platform, including Sales Order specific tests and generic architecture validation tests.

## Test Structure

### 1. Architecture Tests (Generic - Works for ANY Dynamic Screen)

These tests validate the entire dynamic screen system architecture and work for any screen type, not just SalesOrder.

#### 1.1 Dynamic Screen Architecture Tests (`DynamicScreenArchitectureTests.cs`)
Validates that ANY dynamic screen follows the project architecture:
- **Schema Structure Tests**
  - Schema must have required fields (TenantId, Module, Version, ObjectType, Fields)
  - ObjectType must be valid (Master, Transaction, Document, Configuration)
  - Version must be supported (v1-v7)
  - Fields must have valid rules and storage types
  - UI hints must be valid

- **Template Configuration Tests**
  - CalculationRules optional but validated if present
  - DocumentTotals optional but validated if present
  - AttachmentConfig optional but validated if present
  - CloudStorage optional but validated if present

- **Version Management Tests**
  - Supports versions 1-7
  - Version increment preserves backward compatibility

- **Unique Constraints Tests**
  - Unique constraints must reference existing fields

- **ShouldPost (Auto-Posting) Tests**
  - Transaction screens can have ShouldPost enabled
  - EAV screens default ShouldPost to false

#### 1.2 EAV Storage Tests (`EavStorageTests.cs`)
Validates the Entity-Attribute-Value storage system:
- **ObjectDefinition Table Tests**
  - Required fields (Id, TenantId, ObjectCode, Version, IsActive)
  - Max length validations
  - Can store SchemaJson

- **ObjectField Table Tests**
  - Required fields and foreign keys
  - DataType validation (Text, Number, Date, Boolean)

- **ObjectRecord Table Tests**
  - Required fields and foreign keys
  - Inherits AuditableEntity

- **ObjectRecordAttribute Table Tests**
  - Required fields and foreign keys
  - Value fields store different types (ValueText, ValueNumber, ValueDate, ValueBoolean)
  - Unique constraint on RecordId + FieldId

- **Query Performance Tests**
  - Can handle multiple records (100, 1000, 10000)
  - Can handle multiple attributes per record
  - Query patterns by tenant and definition

- **Data Integrity Tests**
  - Cascade delete behavior
  - Data type validation

- **Multi-Tenancy Tests**
  - Tenant isolation
  - Definition isolation

#### 1.3 Storage Strategy Tests (`StorageStrategyTests.cs`)
Validates Real Table vs EAV decision logic:
- **Real Table Screens**
  - SalesOrder, Material, CostCenter, GLAccount, JournalEntry, StockMovement, EmployeePayroll
  - Use dedicated Supabase tables
  - Have standard fields defined

- **EAV Screens**
  - Vendor, Customer, Project, Campaign, ExpenseClaim, PurchaseRequest, CustomObject
  - Use EAV storage (ObjectDefinitions, ObjectRecords, ObjectRecordAttributes)
  - Fields are dynamic

- **Hybrid Storage Tests**
  - Real table screens can store custom fields in EAV
  - Field.Storage property determines destination (Core vs Extension)

- **Storage Routing Tests**
  - Create operation routes to correct storage
  - Read operation handles hybrid mode
  - Update operation routes fields correctly

- **Migration Tests**
  - Real table screens have migrations
  - EAV screens don't need migrations for new fields

#### 1.4 Generic Projection Tests (`GenericProjectionTests.cs`)
Validates MongoDB projection system for ANY screen type:
- **Projection Event Structure Tests**
  - Event must have AggregateType
  - Event must have AggregateId
  - AggregateId must be valid GUID
  - Topic matches aggregate type

- **MongoDB Document Structure Tests**
  - Document must have _id
  - Document must have TenantId
  - Document must have _projectedAt
  - Collection name includes Entity_ prefix

- **Event-to-Projection Mapping Tests**
  - Can resolve entity type
  - Can extract tenant ID
  - Invalid aggregate type returns null

- **Read Model Consistency Tests**
  - Version must be Int64 (not UInt32)
  - Date fields must be BsonDateTime
  - Upsert replaces existing document

- **Schema Versioning Tests**
  - Projection includes version
  - Backward compatibility

- **Multi-Tenancy Tests**
  - Projection includes tenant ID
  - Same ID different tenants

- **Performance Tests**
  - Can handle many documents
  - Large document serialization

#### 1.5 CQRS Architecture Tests (`CqrsArchitectureTests.cs`)
Validates CQRS pattern implementation:
- **Command Handler Pattern Tests**
  - Must implement IRequestHandler
  - Must return ApiResult
  - Must have TenantId
  - Must be serializable
  - Naming conventions

- **Query Handler Pattern Tests**
  - Must implement IRequestHandler
  - Must return ApiResult
  - Must have TenantId
  - Naming conventions

- **Event Sourcing Tests**
  - Event must have EventType
  - Event must have Timestamp
  - Event must have AggregateId
  - Topic mapping
  - Serialization

- **Outbox Pattern Tests**
  - OutboxMessage must have required fields
  - Status validation (Pending, Published, Failed)
  - Default status is Pending
  - Max length validations
  - Message ordering
  - Pending messages query

- **Write/Read Model Separation Tests**
  - Commands modify data
  - Queries don't modify data
  - SQL is write model (source of truth)
  - MongoDB is read model
  - Outbox message created on command

- **Idempotency Tests**
  - Command must have idempotency key
  - Same key returns same result

- **Transaction Boundary Tests**
  - Command handler uses unit of work
  - SaveChanges includes outbox
  - All-or-nothing behavior

#### 1.6 Template Config Validation Tests (`TemplateConfigValidationTests.cs`)
Validates template configuration for ANY screen:
- **CalculationRules Tests**
  - Is optional
  - Must have ServerSide or ClientSide if present
  - LineItemCalculation must have TargetField
  - DocumentCalculation must have TargetField
  - Trigger validation (onChange, onLineChange, onSave, onLoad)
  - ComplexCalculation must have Id
  - Scope validation (LineItem, Document)
  - Can have parameters
  - Can have external data sources
  - Can have code block

- **DocumentTotals Tests**
  - Is optional
  - Must have Fields if present
  - TotalFieldConfig must have Source
  - DecimalPlaces defaults to 2
  - ReadOnly defaults to true
  - DisplayConfig must have Layout and Position

- **AttachmentConfig Tests**
  - Is optional
  - Must have DocumentLevel and LineLevel if present
  - DocumentLevel defaults (Enabled=false, MaxFiles=10, MaxFileSizeMB=50)
  - LineLevel defaults (Enabled=false, MaxFiles=3, MaxFileSizeMB=10)
  - Categories must have Id and Label
  - AllowedTypes must be valid extensions
  - LineLevel can have GridColumnConfig

- **CloudStorage Tests**
  - Is optional
  - Must have Providers if present
  - Must have exactly one default provider
  - Provider validation (S3, AzureBlob, GCP, Local)
  - S3 config must have BucketName and Region
  - Azure config must have AccountName and Container
  - GCP config must have ProjectId
  - GlobalSettings can have encryption
  - LifecycleRules can be configured

- **ComplexCalculation Flag Tests**
  - When false: no complex calculations
  - When true: can have complex calculations
  - Can reference external assemblies

- **Serialization Tests**
  - Round-trip serialization for all config types

### 2. Sales Order Specific Tests

#### 2.1 Backend API Tests (`SalesOrderApiTests.cs`)
Tests for the backend API endpoints:
- **Schema Retrieval Tests**
  - Schema retrieval for all versions (v1-v7) from `/api/platform/object/SalesOrder/latest`
  - Schema contains calculationRules, documentTotals, attachmentConfig, cloudStorage
  - Version-specific feature flags work correctly

- **Calculation Execution Tests**
  - Line item calculations execute server-side
  - Document calculations execute server-side
  - Complex calculation flag works correctly

- **Document Totals Tests**
  - Document totals calculate server-side
  - Totals display correctly in footer

- **Error Handling Tests**
  - Invalid tenant returns 404
  - Missing headers handled correctly

#### 2.2 Integration Tests (`SalesOrderIntegrationTests.cs`)
End-to-end integration tests:
- **End-to-End Flow**
  - Load schema → Render form → Submit with temp values → Server calculates → Save
  - All 7 versions work correctly

- **Complex Calculation Tests**
  - Complex calculations trigger C# code when `complexCalculation:true`
  - Normal calculations work when `complexCalculation:false`
  - Volume discount tiered calculation

- **Document Totals Integration**
  - Server-side totals calculation
  - Correct values for subTotal, taxTotal, totalDiscount

- **Attachment Tests**
  - Attachment upload UI appears when enabled
  - Storage provider configuration valid

- **Cloud Storage Tests**
  - Cloud storage provider selector appears
  - Provider configuration valid

## Running the Tests

### Run All Tests
```bash
cd backend/Tests
dotnet test
```

### Run Architecture Tests Only
```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~Architecture"
```

### Run Sales Order Tests Only
```bash
cd backend/Tests
dotnet test --filter "FullyQualifiedName~SalesOrder"
```

### Run Specific Test Category
```bash
# Dynamic Screen Architecture
dotnet test --filter "FullyQualifiedName~DynamicScreenArchitecture"

# EAV Storage
dotnet test --filter "FullyQualifiedName~EavStorage"

# Storage Strategy
dotnet test --filter "FullyQualifiedName~StorageStrategy"

# Generic Projections
dotnet test --filter "FullyQualifiedName~GenericProjection"

# CQRS Architecture
dotnet test --filter "FullyQualifiedName~CqrsArchitecture"

# Template Config Validation
dotnet test --filter "FullyQualifiedName~TemplateConfigValidation"
```

## Test Categories

| Category | Description | Files |
|----------|-------------|-------|
| Architecture | Generic tests that work for ANY dynamic screen | `DynamicScreenArchitectureTests.cs`, `EavStorageTests.cs`, `StorageStrategyTests.cs`, `GenericProjectionTests.cs`, `CqrsArchitectureTests.cs`, `TemplateConfigValidationTests.cs` |
| SalesOrder | Sales Order specific tests | `SalesOrderApiTests.cs`, `SalesOrderIntegrationTests.cs`, `SalesOrderCqrsTests.cs`, `SalesOrderKafkaTests.cs`, `SalesOrderMongoIntegrationTests.cs`, `SalesOrderSupabaseTests.cs` |

## Architecture Principles Validated

1. **Dynamic Screens**: Configurable via MongoDB PlatformObjectTemplate
2. **EAV Storage**: Dynamic data in PostgreSQL/Supabase (ObjectDefinitions, ObjectRecords, ObjectRecordAttributes)
3. **Real Tables**: Accounting/ERP data in dedicated Supabase tables (SalesOrder, Material, etc.)
4. **Hybrid Mode**: Real tables + EAV for custom fields
5. **MongoDB Read Model**: Persistent projections for queries
6. **CQRS Pattern**: Command handlers, event sourcing, projections
7. **Outbox Pattern**: Reliable event publishing
8. **Multi-Tenancy**: Tenant isolation at all levels
9. **Version Management**: Schema versions v1-v7 support
