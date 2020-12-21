using System.ComponentModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Vidyano.Service.Repository;

namespace Vidyano.Service
{
    /// <summary>
    /// Provides extension methods for <see cref="IQueryable"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class QueryExtensions
    {
        /// <summary>
        /// Checks if the specified source is an Entity Framework source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public static bool IsEfQuery(this System.Collections.IEnumerable source)
        {
            if (source is IWrappedEnumerable wrappedEnumerable)
                source = wrappedEnumerable.Wrapped;

            var genericArgument = source.GetType().GetGenericArguments()[0];
            if (genericArgument.IsValueType)
                return false;

            return typeof(DbSet<>).MakeGenericType(genericArgument).IsInstanceOfType(source)
#pragma warning disable EF1001 // Internal EF Core API usage.
                || typeof(EntityQueryable<>).MakeGenericType(genericArgument).IsInstanceOfType(source)
#pragma warning restore EF1001 // Internal EF Core API usage.
                ;
        }
    }
}