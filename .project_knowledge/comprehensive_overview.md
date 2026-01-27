# Valora Project - Comprehensive Knowledge Base

**Date:** 2026-01-26
**Scope:** Full Stack (Frontend + Backend)

## 1. Project Overview
Valora is a dynamic, multi-tenant SaaS platform built to support flexible data modeling, custom forms, and dynamic workflows (likely for healthcare/lab management given terms like "Patient", "Pathology", "Doctor"). It features a "Low-Code" style Studio for defining schemas and a Runtime App for end-users.

---

## 2. Architecture & Patterns

### **Backend (`Valora.Api`)**
*   **Framework:** .NET 8.0 / 10.0 (Preview/RC)
*   **Pattern:** Clean Architecture + CQRS (Command Query Responsibility Segregation) using MediatR.
*   **Database Strategy (Hybrid/Polyglot):**
    *   **PostgreSQL (Supabase):** The **Source of Truth** for transactional business data.
        *   **OLD Strategy:** Dynamic Tables (e.g., `patients`, `doctors`) created on the fly via DDL.
        *   **NEW Strategy (EAV - Entity Attribute Value):** Fixed Schema using Metadata + Attribute Tables.
            *   `ObjectDefinition`: Stores module metadata (Code, Version, Tenant).
            *   `ObjectField`: Stores field definitions.
            *   `ObjectRecord`: The "Entity" instance (Id, Tenant, ObjectId, Audit fields).
            *   `ObjectRecordValue`: The actual data (RecordId, FieldId, ValueString, ValueNumber, ValueDate, ValueBoolean).
    *   **MongoDB:** Stores **Metadata & Schemas** (`PlatformObjectTemplate`). Also likely used as a Read Model/Cache for high-performance reads (Eventual Consistency).
    *   **EF Core:** Manages system tables (`OutboxMessages`, `EntityExtensions`) and migrations.
    *   **Dapper:** Used in `DynamicRepository` for high-performance raw SQL writes (essential for dynamic tables where EF Core models don't exist).
*   **Event-Driven Consistency:**
    *   **Outbox Pattern:** Writes to `OutboxMessages` table in the same transaction as business data.
    *   **Kafka:** Consumes messages (`KafkaConsumer`) to trigger async tasks (e.g., Schema Sync, Read Model updates).

### **Frontend (`frontend`)**
*   **Framework:** React 18 + Vite.
*   **Language:** TypeScript.
*   **Styling:** Tailwind CSS (utility-first).
*   **State Management:** React Context (`ThemeContext` for global UI state).
*   **Form Engine:** `react-hook-form` (highly performant forms).
*   **Editor:** Monaco Editor (VS Code core) for JSON schema editing in Studio.

---

## 3. Key Components & Logic

### **Backend Internals**
1.  **Schema Management (`ModuleSchema`)**:
    *   Defines the structure of an entity (Fields, Types, UI Hints).
    *   **Storage:** JSON in MongoDB (`PlatformObjectTemplate`).
    *   **Validation:** `SchemaValidator` enforces rules (Required, MaxLength) before writing to SQL.
2.  **Dynamic Writes (`CreateEntityCommand`)**:
    *   **Flow:** Validate JSON -> Split into Core vs. Extension -> Begin Transaction -> Insert Core (SQL) -> Insert Extensions (SQL) -> Write Outbox Event -> Commit.
    *   **Field Splitting:** Fields marked `Storage: "Core"` go to specific columns. Others go to the `entity_extensions` table (Key-Value store).
3.  **Schema Sync (`SchemaSyncService`)**:
    *   Listens for schema changes.
    *   **OLD:** Executed Raw SQL DDL (`CREATE TABLE`, `ALTER TABLE`) to keep Postgres tables in sync with the JSON schema definitions.
    *   **NEW:** Syncs metadata rows into `ObjectDefinition` and `ObjectField` tables. No DDL operations.

### **Frontend Internals**
1.  **Platform Studio (`PlatformStudio.tsx`)**:
    *   The "IDE" for the platform.
    *   **Split View:** Left side is Monaco JSON editor; Right side is **Live Preview**.
    *   **Features:** Create/Clone modules, Manage Versions, Publish schemas.
    *   **Normalization:** Uses `normalizeSchema` (`schemaUtils.ts`) to make raw JSON (and API envelopes) consumable by the UI.
2.  **Dynamic Form Engine (`DynamicForm.tsx`)**:
    *   **Generic Rendering:** Renders fields based on the schema *at runtime*. No hardcoded forms.
    *   **Grid Support:** Supports inline grids (lines items) via `FormGrid.tsx`.
    *   **Intelligent Layout:** Can auto-generate layouts if not provided, or respect explicit `ui.layout`.
    *   **Features:** Lookups (fetching related data), Sequences (auto-numbering), Validation (client-side).
3.  **Grid System (`FormGrid.tsx`)**:
    *   Handles complex nested data (e.g., Lab Test Items inside a Bill).
    *   Supports inline editing, calculations (Tax, Net Amount), and lookups.

---

## 4. Critical Configurations

*   **Database Connections (`appsettings.json`):**
    *   `WriteConnection`: Postgres (Supabase) - Port 5432, Pooling Enabled.
    *   `MongoDb`: Connection string for metadata store.
    *   `Kafka`: Bootstrap servers for event bus.
*   **Frontend Config:**
    *   `vite.config.ts`: Configured for React, uses `jsdom` for testing.
    *   `tailwind.config.js`: Custom theme colors (Valora brand).

## 5. Development & Testing
*   **Testing:** `vitest` used for frontend unit tests (verified `DynamicForm` logic).
*   **Tools:**
    *   `SchemaSeeder`: Tool to seed initial MongoDB templates.
    *   `TestKafkaConnection`: Utility to verify event bus connectivity.

---

## 6. Minute Details (Memorized)
*   **Field Resolution:** The frontend uses a fuzzy match strategy (suffix matching) to link Layout definitions (short names) to Schema definitions (flattened dot-notation names) to ensure robust rendering.
*   **Implicit Grids:** If a field has a `columns` property but no explicit `type: "grid"`, the system now treats it as a grid automatically (Functional Test Verified).
*   **API Envelopes:** The frontend normalizer automatically unwraps `{ success: true, data: ... }` responses to prevent editor/preview crashes.
*   **Audit Fields:** Every dynamic table automatically gets `Id`, `TenantId`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`.
*   **Tenancy:** Multi-tenancy is enforced at the database level (TenantId column) and context level (`TenantContext`).

**Storage Location:** This file is saved at `.project_knowledge/comprehensive_overview.md`.
