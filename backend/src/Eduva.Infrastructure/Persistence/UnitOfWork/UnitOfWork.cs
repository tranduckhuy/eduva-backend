using Eduva.Domain.Common;
using Eduva.Domain.Interfaces;
using Eduva.Domain.Interfaces.Repositories;
using Eduva.Infrastructure.Persistence.DbContext;
using Eduva.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Eduva.Infrastructure.Persistence.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _currentTransaction;
        private bool _disposed = false;

        public ILessonMaterialRepository LessonMaterialRepository { get; private set; }
        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            LessonMaterialRepository = new LessonMaterialRepository(_context);
        }

        #region Repository Properties

        #endregion

        #region Transaction Management

        public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
                throw new InvalidOperationException("A transaction is already in progress.");

            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return _currentTransaction.GetDbTransaction();
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
                throw new InvalidOperationException("No transaction in progress.");

            try
            {
                await _currentTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
                throw new InvalidOperationException("No transaction in progress.");

            try
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        #endregion

        #region Save Operations

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Apply audit fields for timestamped entities
                ApplyAuditInformation();
                
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Handle concurrency conflicts
                throw new InvalidOperationException("The entity was modified by another user. Please refresh and try again.", ex);
            }
            catch (DbUpdateException ex)
            {
                // Handle database update exceptions
                throw new InvalidOperationException("An error occurred while saving changes to the database.", ex);
            }
        }

        public async Task<bool> CompleteAsync(CancellationToken cancellationToken = default)
        {
            var result = await SaveChangesAsync(cancellationToken);
            return result > 0;
        }

        private void ApplyAuditInformation()
        {
            var entries = _context.ChangeTracker.Entries()
                .Where(e => e.Entity is BaseTimestampedEntity<int> || e.Entity is BaseTimestampedEntity<Guid>)
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity is BaseTimestampedEntity<int> intEntity)
                    {
                        intEntity.CreatedAt = DateTimeOffset.UtcNow;
                    }
                    else if (entry.Entity is BaseTimestampedEntity<Guid> guidEntity)
                    {
                        guidEntity.CreatedAt = DateTimeOffset.UtcNow;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (entry.Entity is BaseTimestampedEntity<int> intEntity)
                    {
                        intEntity.LastModifiedAt = DateTimeOffset.UtcNow;
                    }
                    else if (entry.Entity is BaseTimestampedEntity<Guid> guidEntity)
                    {
                        guidEntity.LastModifiedAt = DateTimeOffset.UtcNow;
                    }
                }
            }
        }

        #endregion

        #region Dispose Pattern

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _currentTransaction?.Dispose();
                _context.Dispose();
                _disposed = true;
            }
        }

        ~UnitOfWork()
        {
            Dispose(false);
        }

        #endregion
    }
}