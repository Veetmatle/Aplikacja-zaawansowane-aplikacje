# ADR-001: IUnitOfWork Interface in Core Layer

**Status:** Accepted  
**Date:** 2025-01-15  
**Context:** ShopApp Clean Architecture — placement of transactional abstractions

---

## Context

ShopApp follows Clean Architecture (Onion) with strict Dependency Rule:
`Core ← Application ← Infrastructure ← API`

The `PaymentService` in Application layer needs to update `Payment` and `Order` entities
atomically within a single database transaction when processing P24 payment notifications.
Without transactional guarantees, a server crash between the two updates could leave the
system in an inconsistent state (payment marked as completed, but order still pending).

## Decision

We placed `IUnitOfWork` interface in `ShopApp.Core.Interfaces`:

```csharp
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
```

The implementation (`UnitOfWork`) lives in `ShopApp.Infrastructure.Repositories` and wraps
EF Core's `IDbContextTransaction`.

## Rationale

### Why not Domain Events?

Domain Events (publish event → Infrastructure handles transactionally) is the purist
approach, but introduces significant complexity:
- Requires an event dispatcher, event handlers, and likely MediatR.
- For a single use case (payment notification), the overhead is disproportionate.
- Domain Events are better suited when multiple bounded contexts react to the same event.

### Why is this acceptable?

1. **Interface, not implementation.** Core defines *what* (begin/commit/rollback), not *how*
   (EF Core, ADO.NET, MongoDB). The abstraction doesn't leak database-specific concepts.

2. **Pragmatic scope.** Only `PaymentService` uses `IUnitOfWork`. If more services need it,
   that's a signal to reconsider the pattern — not a current problem.

3. **Testable.** Unit tests mock `IUnitOfWork` via NSubstitute — no infrastructure dependency.

4. **Widely used in .NET community.** Microsoft's own eShopOnContainers reference architecture
   places `IUnitOfWork` in the Domain layer.

### The counter-argument

Purists argue that `IUnitOfWork` is a "leaking abstraction" — Core shouldn't know that
transactional consistency exists. In theory, Core should only express business invariants,
and the Application layer should orchestrate without knowing about transactions.

This is valid for large-scale systems with multiple bounded contexts. For a single-process
monolith like ShopApp, the pragmatic approach wins.

## Consequences

### Positive
- Payment notification processing is atomic (Payment + Order updated together).
- Clear, testable interface — no EF Core dependency in Application.
- Minimal code change to add transactional support to other services if needed.

### Negative
- Core layer has awareness of transactional semantics (BeginTransaction/Commit/Rollback).
- If ShopApp evolves to event-driven architecture, `IUnitOfWork` should be replaced
  with Domain Events + Outbox Pattern.

### Monitoring
- If more than 2-3 services start depending on `IUnitOfWork`, reconsider introducing
  MediatR with a transactional pipeline behavior.

---

## Related
- `ShopApp.Core/Interfaces/IUnitOfWork.cs` — interface definition
- `ShopApp.Infrastructure/Repositories/UnitOfWork.cs` — EF Core implementation
- `ShopApp.Application/Services/PaymentService.cs` — consumer (HandleNotificationAsync)
