# ADR-002: Soft Delete for ApplicationUser (IdentityUser)

**Status:** Accepted  
**Date:** 2025-01-15  
**Context:** ShopApp — consistent soft-delete across all entities including Identity users

---

## Context

ShopApp uses soft delete (`DeletedAt` field + EF Core `GlobalQueryFilter`) for all domain
entities via `BaseEntity`. However, `ApplicationUser` inherits from `IdentityUser<Guid>` 
(ASP.NET Core Identity), not from `BaseEntity`. This created an inconsistency:

- Items, Orders, Categories, Payments — soft-deleted (recoverable, auditable)
- Users — hard-deleted (data permanently lost, breaks referential integrity for Orders)

## Decision

Add `DeletedAt` and `IsDeleted` properties directly to `ApplicationUser` (since C# doesn't
support multiple inheritance, we can't also inherit from `BaseEntity`):

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    // ... existing properties ...
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt is not null;
}
```

Add a `GlobalQueryFilter` in `AppDbContext`:
```csharp
builder.Entity<ApplicationUser>().HasQueryFilter(u => u.DeletedAt == null);
```

Change `AdminUserService.DeleteUserAsync` from `_userManager.DeleteAsync()` (hard delete)
to setting `DeletedAt = DateTime.UtcNow` + `_userManager.UpdateAsync()` (soft delete).

## Consequences

### Positive
- Consistent soft-delete semantics across all entities.
- Deleted users' data is preserved for audit, order history, and potential account recovery.
- `GlobalQueryFilter` automatically excludes soft-deleted users from all Identity queries
  (`FindByEmailAsync`, `FindByIdAsync`, etc.) — deleted users can't login.

### Negative
- Unique email constraint: soft-deleted users still occupy their email in the DB. If a user
  wants to re-register with the same email, we'd need to handle this (e.g., append a suffix
  to deleted user's email, or use `IgnoreQueryFilters()` with special logic).
- `ApplicationUser` duplicates the `DeletedAt`/`IsDeleted` pattern from `BaseEntity`. This
  is unavoidable due to the `IdentityUser` inheritance constraint.

### Future consideration
- If account recovery is needed, add an admin endpoint with `.IgnoreQueryFilters()` to list
  and restore soft-deleted users.

---

## Related
- `ShopApp.Core/Entities/ApplicationUser.cs` — DeletedAt property
- `ShopApp.Infrastructure/Data/AppDbContext.cs` — GlobalQueryFilter for ApplicationUser
- `ShopApp.Application/Services/AdminUserService.cs` — soft-delete in DeleteUserAsync
