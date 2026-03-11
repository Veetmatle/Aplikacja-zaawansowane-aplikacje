using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ShopApp.Core.Common;
using ShopApp.Core.Interfaces.Repositories;
using ShopApp.Infrastructure.Data;

namespace ShopApp.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FindAsync(new object[] { id }, ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.Where(predicate).ToListAsync(ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        var entry = _context.Entry(entity);
        
        if (entry.State == EntityState.Detached)
        {
            // Detached entity — attach and mark as Modified
            _dbSet.Update(entity);
        }
        else
        {
            // Already tracked — detect new children added to navigation properties.
            // Run DetectChanges so EF picks up new entities added to tracked collections.
            _context.ChangeTracker.DetectChanges();
        }

        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Soft-delete for BaseEntity-derived entities (sets DeletedAt).
    /// Hard-delete for other types.
    /// </summary>
    public async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.DeletedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
        }
        else
        {
            _dbSet.Remove(entity);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.AnyAsync(predicate, ct);
}
