using System;
using System.Collections;
using Microsoft.EntityFrameworkCore;
using Vidyano.Service.Repository;

namespace Vidyano.Service
{
    /// <summary>
    /// Provides a base class for <see cref="DbContext"/> to implement <see cref="ITargetContext"/>.
    /// </summary>
    public abstract class TargetDbContext : DbContext, ITargetContext, ITargetContextGetEntitySource
    {
        /// <inheritdoc />
        protected TargetDbContext()
        {
        }

        /// <inheritdoc />
        protected TargetDbContext(DbContextOptions options)
            : base(options)
        {
        }

        /// <inheritdoc />
        public virtual void MarkForDeletion<TEntity>(TEntity entity)
        {
            Remove(entity ?? throw new ArgumentNullException(nameof(entity)));
        }

        /// <summary>
        /// Fallback method to find the source for the specified <paramref name="obj"/> if no <see cref="DbSet{TEntity}"/> property exists.
        /// </summary>
        public virtual IEnumerable? GetEntitySource(PersistentObject obj)
        {
            return null;
        }
    }
}