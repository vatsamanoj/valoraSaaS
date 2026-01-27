# Valora Platform Policies

This document serves as the **Single Source of Truth** for all architectural, operational, and development policies within the Valora platform. It is aggregated from the granular policy definitions in `.trae/policies/`.

**Platform Identity**: SAP-like Global SaaS ERP.
**Architecture**: Clean Architecture + CQRS + Polyglot Persistence.
**Primary Goal**: Correctness, Compliance, and Scalability over convenience.

---

## 1. Core Principles & Architecture

### 1.1 The "3 Pillars" Storage Strategy
Valora uses a strict polyglot persistence model. Data location is determined by its nature, not convenience.

| Store | Use Case | Characteristics | Policy |
|-------|----------|-----------------|--------|
| **SQL (Supabase)** | **The Truth**. Financials, Ledger, Transactions, Compliance, Master Data. | Fixed Schema. Strong Consistency. Foreign Keys. Immutable after posting. | **NEVER** use JSON columns for financial amounts. **NEVER** use dynamic DDL. |
| **EAV (Postgres)** | **The Flexibility**. Dynamic UI fields, Tenant-specific attributes, Non-financial masters. | Tables: `ObjectRecord`, `ObjectField`. Dynamic schema at runtime. | **NEVER** store Money, Ledger, or Payroll values here. Isolated from Core FI/MM. |
| **Mongo** | **The View**. Dashboards, Search, Analytics, Read-heavy screens. | Denormalized. Read-optimized. Eventually Consistent. | **NEVER** the source of truth. **READ-ONLY** from API. Updated **ONLY** via Kafka Events. |

### 1.2 Tenancy & Security (Zero Trust)
- **Logical Isolation**: Single Database, Shared Schema.
- **TenantId Enforcement**: **MANDATORY** on every table, document, and event.
- **Row-Level Security (RLS)**: Enabled on all tables. Enforces isolation at the database engine level.
- **Authentication**: JWT mandatory. User identity and TenantId derived strictly from Auth Context (never trusted from client payloads).

### 1.3 Event-Driven Architecture
- **Pattern**: **Transactional Outbox**.
- **Rule**: Business logic **NEVER** publishes to Kafka directly.
- **Flow**: `Domain Logic` -> `SQL Transaction` -> `Outbox Table` -> `Outbox Processor` -> `Kafka` -> `Consumer` -> `Mongo Read Model`.
- **Topics**: Tenant-scoped (e.g., `tenant.module.event`).

### 1.4 "Smart Policy": Definition of Done (CQRS Completeness)
To ensure system consistency, **EVERY** new Entity or Module implementation MUST verify the full "Write-to-Read" loop before being marked as Complete:
1.  **Write Path**: Command -> Handler -> SQL Entity -> Outbox Event (in same Transaction).
2.  **Eventing**: Event Class defined -> Outbox Processor picks it up -> Kafka Topic exists.
3.  **Read Path**: Kafka Consumer subscribes to Topic -> Projection Manager handles Event -> MongoDB Collection updated.
4.  **Verification**: Test must confirm that a Write to SQL results in a visible update in MongoDB (Read Model).
*Failure to implement the Read Path (Projections) is a Critical Defect.*

---

## 2. Core Business Modules (SAP-Style)

### 2.1 Finance (FI) - "The Core"
*   **Role**: Owns the money. The absolute source of financial truth.
*   **Model**: Double-Entry Ledger.
*   **Rules**:
    *   **Immutable**: Posted documents cannot be edited. Corrections require reversal documents.
    *   **Fixed Schema**: No EAV for amounts.
    *   **Currency**: Stored in Tenant Base Currency. Exchange rates captured at posting time.

### 2.2 Controlling (CO) - "The Analytics"
*   **Role**: Explains the money (Cost & Profitability Analysis).
*   **Model**: Derived from FI.
*   **Rules**:
    *   **Read-Only**: Reads from FI ledger. Never writes to ledger directly.
    *   **Allocations**: Creates FI adjustment entries.
    *   **Period Control**: Respects FI posting periods.

### 2.3 Sales & Distribution (SD) - "The Revenue"
*   **Role**: Manages Order-to-Cash.
*   **Flow**: `SalesOrder` -> `Delivery` -> `Invoice`.
*   **Rules**:
    *   **Billing**: The point of financial impact. Creates FI posting.
    *   **Pricing**: Locked upon confirmation/billing.
    *   **Integration**: Delivery triggers MM goods issue. Invoice triggers FI revenue posting.

### 2.4 Materials Management (MM) - "The Inventory"
*   **Role**: Manages Procure-to-Pay and Stock.
*   **Flow**: `PurchaseOrder` -> `GoodsReceipt` -> `InvoiceVerification`.
*   **Rules**:
    *   **Stock**: Quantity first, Value second (derived from FI).
    *   **Movements**: Every movement (GRN, Issue, Transfer) creates a document and FI ledger entry.
    *   **Valuation**: Moving Average or Standard Price, strictly governed.

### 2.5 Human Capital (HCM) - "The People"
*   **Role**: Manages Employee Lifecycle and Payroll.
*   **Rules**:
    *   **PII**: Strictly protected and encrypted.
    *   **Payroll**: Period-based calculation. Results are immutable after posting.
    *   **Integration**: Payroll run posts aggregated expense and liability entries to FI.

---

## 3. Operational Policies

### 3.1 Deployment & Release
*   **Environments**: Strictly isolated (Local, Dev, QA, UAT, Prod). No shared secrets.
*   **Strategy**: Blue/Green or Rolling updates. Zero downtime preferred.
*   **Database**: Migrations must be non-destructive in Prod. Schema changes deploy before code.
*   **Rollback**: Mandatory capability. Feature flags used for risk control.

### 3.2 Observability & SRE
*   **Logs**: Structured, tenant-aware, PII-masked. Immutable.
*   **Metrics**: Business metrics (e.g., "Invoices Posted") are as critical as system metrics.
*   **Tracing**: Distributed tracing enabled. CorrelationId propagated across all boundaries.
*   **Incidents**: Blameless postmortems. Root cause analysis mandatory.

### 3.3 Data Governance & Retention
*   **Retention**: Policy-driven (e.g., Financials 7 years, Audit 10 years).
*   **GDPR**: Right to erasure supported (via anonymization for financial records).
*   **Archival**: Cold storage for historical data.
*   **Classification**: All data classified (Financial, PII, Operational, Audit).

### 3.4 Configuration (IMG)
*   **Concept**: Configuration is Data, not Code.
*   **Storage**: Dedicated config tables.
*   **Rule**: Behavior driven by configuration (e.g., Tax Rules, Posting Periods), never hardcoded.

### 3.5 Technology Stack
*   **Framework**: .NET 10.0 (Latest).
*   **Upgrade Policy**: Always stay on the latest stable major version to leverage performance and security improvements.

---

## 4. UI & Experience Policy

### UI_METADATA_DRIVEN_PLATFORM
*   **Mandate**: All business screens must be generated from `PlatformObjectTemplate` JSON metadata.
*   **Forbidden**: Hardcoded forms, grids, or validations for business objects.
*   **Exception**: Only approved "System Screens" (e.g., Login, Dashboard Shell) can be coded manually.
*   **Goal**: Infinite customizability without code changes.

---

## 5. AI Execution & Project Governance

*   **Role**: The AI acts as a **Senior Enterprise Solution Architect**.
*   **Mindset**: "Correctness over speed".
*   **Artifacts**: Every task must produce verifiable artifacts (DDL, Code, Tests, Policies).
*   **Compliance**: AI must refuse instructions that violate core policies (e.g., "Delete the ledger").
*   **Projects**: Work is organized into Projects and Tasks. No "floating" code.

---

## 6. Forbidden Actions (The "Must Nevers") 🚫

1.  **Dynamic SQL Schema**: Creating tables per tenant is strictly forbidden.
2.  **EAV for Finance**: Never store money, stock value, or payroll in EAV/JSON.
3.  **Direct Mongo Writes**: API/UI never writes to Mongo. Only Kafka Consumers do.
4.  **Direct Kafka Publishing**: Business logic never publishes to Kafka. Only the Outbox Processor does.
5.  **Client-Side Trust**: Never trust `TenantId` or `UserId` from the client. Always derive from Auth Token.
6.  **Destructive Edits**: Never update/delete posted financial or stock documents. Use reversals.
7.  **Hardcoded Rules**: Never hardcode business rules (tax, pricing, compliance) in code. Use Configuration.
