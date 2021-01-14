using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Vidyano.Core.Services;
using Vidyano.Service.EntityFrameworkCore.Metadata;
using Vidyano.Service.Metadata;
using Vidyano.Service.Repository;

namespace Vidyano.Service.EntityFrameworkCore
{
    public class EntityFrameworkCoreSpecificService : ProviderSpecificService
    {
        public override bool NeedsSimplifiedExpression(IQueryable source) => source.IsEfQuery();

        /// <inheritdoc />
        public override bool HandleIncludes<T>(ref IQueryable<T> queryable, ITypeCache type, IEnumerable<QueryColumn> columns)
            where T : class
        {
            var changed = false;
            if (!type.HasProjectedTypeAttribute && queryable is DbSet<T>)
            {
                foreach (var refAttr in columns.Where(c => c.IsHidden != true).Select(c => c.Attribute).OfType<PersistentObjectAttributeWithReference>())
                {
                    var property = type.GetProperty(refAttr.Name);
                    if (property != null && property.IsMapped)
                    {
                        queryable = queryable.Include(refAttr.Name);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        public override Type GetObjectType(Type type)
        {
            if (typeof(Castle.DynamicProxy.IProxyTargetAccessor).IsAssignableFrom(type))
                type = type.BaseType!;

            return type;
        }

        public override EntityModel GetEntityModel(Type contextType)
        {
            if (!typeof(DbContext).IsAssignableFrom(contextType))
                return base.GetEntityModel(contextType);

            using var localScope = ServiceLocator.GetScopedRequiredService(contextType, out var service);
            if (!(service is DbContext dbContext))
                return base.GetEntityModel(contextType);

            var entityModel = new EntityFrameworkCoreEntityModel(contextType, dbContext);
            entityModel.DiscoverEntitiesFromContext(contextType);
            return entityModel;
        }
    }
}