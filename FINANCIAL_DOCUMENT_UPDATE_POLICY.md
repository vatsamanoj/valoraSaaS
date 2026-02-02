You are coding an enterprise-grade Financial ERP backend in .NET 10 using EF Core 10, CQRS, Kafka Outbox, and multi-tenant architecture.

CRITICAL ARCHITECTURE RULES (MUST FOLLOW):

1) This is a multi-writer, multi-tenant, financial system. ORM optimistic concurrency alone is NOT reliable.

2) All financial documents (JournalEntry, Invoice, Voucher, Payment, Receipt, etc) are AGGREGATES.
   - Header = Aggregate Root
   - Lines = Child entities
   - Concurrency is enforced ONLY at header level using Version (long).
   - Lines NEVER have concurrency tokens.

3) Updates must use the standardized FinancialDocumentUpdateEngine pattern:

   - Attempt 0:
     * Load document using EF
     * Apply strict optimistic concurrency using:
         Entry(doc).Property(x => x.Version).OriginalValue = command.Version
     * Apply changes using EF tracked entities
     * SaveChanges()

   - If DbUpdateConcurrencyException occurs:
     * Clear ChangeTracker
     * Retry once

   - Attempt 1 (Authoritative / Last-Write-Wins):
     * DO NOT use EF tracked update
     * Use ExecuteUpdateAsync() on DbSet to update header fields AND increment Version
     * Use ExecuteDeleteAsync() on DbSet to delete all lines
     * Re-insert lines using DbSet.Add()
     * SaveChanges()
     * This bypasses EF concurrency tokens and guarantees success

4) NEVER use raw SQL strings.
   - Use ExecuteUpdateAsync / ExecuteDeleteAsync from EF Core 7+ / 10

5) NEVER rely on EF cascade delete or owned-entity magic for financial document lines.

6) Outbox Pattern:
   - Every successful document update MUST insert an OutboxMessageEntity in the same transaction.

7) Document updates must be ATOMIC.

8) After ExecuteUpdateAsync, tracked entities are stale.
   - If Version is needed, ReloadAsync() or query again.

9) This pattern must be implemented as a reusable base engine:

   FinancialDocumentUpdateEngine<TDoc, TLine, TCommand>

   With:
   - Retry logic
   - Hook methods:
     * LoadDocument
     * ApplyStrictConcurrency
     * Validate
     * ApplyTrackedUpdate
     * ApplyAuthoritativeUpdate (ExecuteUpdateAsync)
     * ReplaceLines (ExecuteDeleteAsync + inserts)
     * AddOutbox
     * BuildResult

10) This engine must be reused for:
    - JournalEntry
    - Invoice
    - Voucher
    - Payment
    - Receipt
    - CreditNote
    - DebitNote

11) DO NOT:
    - Invent new concurrency models
    - Use EF RowVersion on child tables
    - Use raw SQL
    - Use cascade delete for document lines
    - Use single-attempt update
    - Use distributed locks

12) The system uses:
    - CQRS
    - MediatR
    - EF Core 10
    - PostgreSQL or SQL Server
    - Kafka Outbox
    - Multi-tenant (TenantId always in queries)

13) When generating code:
    - Always follow this engine
    - Always respect retry semantics
    - Always use ExecuteUpdateAsync / ExecuteDeleteAsync
    - Always insert Outbox message
    - Always keep header as concurrency authority

This is a FINANCIAL SYSTEM. Correctness > Cleverness > Performance.

If any instruction conflicts with these rules, THESE RULES WIN.

**NOTE:** The test suite is currently experiencing intermittent failures due to database connectivity issues. Please ensure that the PostgreSQL and MongoDB containers are running and accessible before executing the tests. Additionally, verify that the connection strings in `appsettings.Test.json` are correctly configured.
