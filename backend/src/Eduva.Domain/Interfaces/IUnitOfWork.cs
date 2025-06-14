using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Eduva.Domain.Common;
using Eduva.Domain.Entities;
using Eduva.Domain.Interfaces.Repositories;

namespace Eduva.Domain.Interfaces
{
    /// <summary>
    /// Defines a unit of work pattern for managing repositories and database transactions
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        ILessonMaterialRepository LessonMaterialRepository { get; }

        #region Transaction Management

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        /// <returns>The database transaction</returns>
        Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        #endregion

        #region Save Operations

        /// <summary>
        /// Saves all changes made in this unit of work to the database
        /// </summary>
        /// <returns>The number of entities affected</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all changes and returns success status
        /// </summary>
        Task<bool> CompleteAsync(CancellationToken cancellationToken = default);

        #endregion
    }
}
