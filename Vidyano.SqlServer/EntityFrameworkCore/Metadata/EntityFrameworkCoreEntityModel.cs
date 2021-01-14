using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Vidyano.Core.Services;
using Vidyano.Service.Metadata;
using Vidyano.Service.Repository;

namespace Vidyano.Service.EntityFrameworkCore.Metadata
{
    public class EntityFrameworkCoreEntityModel : EntityModel
    {
        private readonly Type contextType;
        private readonly Dictionary<string, IEntityType> dbEntityTypes;

        public EntityFrameworkCoreEntityModel(Type contextType, DbContext dbContext)
        {
            this.contextType = contextType;

            dbEntityTypes = dbContext.Model.GetEntityTypes().ToDictionary(et => et.Name);
        }

        protected override EntityType? CreateEntityType(Type targetContextType, Type type, ITypeCache typeCache, string? contextProperty)
        {
            var dbEntityType = dbEntityTypes.Values.FirstOrDefault(et => et.ClrType == type);
            if (dbEntityType != null)
            {
                var entityType = new EntityType(type, contextProperty);
                var properties = dbEntityType.GetProperties();
                entityType.Properties = properties.Select(CreateProperty).ToArray();
                var primaryKey = dbEntityType.GetKeys().FirstOrDefault(k => k.IsPrimaryKey());
                if (primaryKey != null)
                    entityType.KeyProperties = primaryKey.Properties.Select(CreateProperty).ToArray();
                entityType.NavigationProperties = dbEntityType.GetNavigations().Select(CreateNavigationProperty).ToArray();
                return entityType;
            }

            return base.CreateEntityType(targetContextType, type, typeCache, contextProperty);
        }

        private static EntityProperty CreateProperty(IProperty property)
        {
            return new EntityFrameworkEntityProperty(property);
        }

        private static EntityNavigationProperty CreateNavigationProperty(INavigation navigation)
        {
            var isCollection = navigation.IsCollection();
            var isRequired = navigation.ForeignKey.IsRequired;
            var navigationProperty = new EntityNavigationProperty(navigation.Name, isCollection ? navigation.ClrType.GetItemType() : navigation.ClrType, isCollection, !isRequired);
            if (!isCollection)
            {
                var properties = navigation.ForeignKey.Properties;
                navigationProperty.DependentPropertyNames = properties.Select(p => p.Name).ToArray();
            }
            return navigationProperty;
        }

        public override bool PostSynchronize()
        {
            using var localScope = ServiceLocator.GetScopedRequiredService(contextType, out var service);
            var dbContext = (DbContext)service!;

            return dbContext.Database.ExecuteSqlRaw(SqlStatements.PostSynchronize) == 1; // TODO: Does not return correct number
        }
    }
}